using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;
        
        public MessageRepository(DataContext context, IMapper mapper)
        {
            this.mapper = mapper;
            this.context = context;
        }

        public void AddMessage(Message message) => 
            context.Messages.Add(message);

        public void DeleteMessage(Message message) => 
            context.Messages.Remove(message);

        public async Task<Message> GetMessageAsync(int id) =>
            await context.Messages.FindAsync(id);

        public async Task<PagedList<MessageDTO>> GetMessagesForUserAsync(
            MessageParams messageParams)
        {
            var query = context.Messages
                .OrderByDescending(m => m.MessageSent)
                .AsQueryable();

            query = messageParams.Container switch 
            {
                "inbox" => query.Where(u => u.Recipient.UserName == messageParams.UserName),
                "Outbox" => query.Where(u => u.Sender.UserName == messageParams.UserName),
                _ => query.Where(u => u.Recipient.UserName == messageParams.UserName && 
                    u.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDTO>(mapper.ConfigurationProvider);

            return await PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber,
                messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDTO>> GetMessageThreadAsync(
            string currentUserName, string recipientUserName)
        {
            var messages = await context.Messages
                .Include(u => u.Sender)
                .ThenInclude(p => p.Photos)
                .Include(u => u.Recipient)
                .ThenInclude(p => p.Photos)
                .Where(m => m.Recipient.UserName == currentUserName && 
                    m.Sender.UserName == recipientUserName || 
                    m.Recipient.UserName == recipientUserName && 
                    m.Sender.UserName == currentUserName)
                .OrderBy(m => m.MessageSent)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null &&
                m.Recipient.UserName == currentUserName).ToList();

            if (unreadMessages.Any()) 
            {
                foreach (var message in unreadMessages) 
                {
                    message.DateRead = DateTime.Now;
                }

                await context.SaveChangesAsync();
            }

            return mapper.Map<IEnumerable<MessageDTO>>(messages);
        }

        public async Task<bool> SaveAllAsync() => 
            await context.SaveChangesAsync() > 0;
    }
}