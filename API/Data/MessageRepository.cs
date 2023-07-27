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

        public void AddGroup(Group group) =>
            context.Groups.Add(group);

        public void AddMessage(Message message) => 
            context.Messages.Add(message);

        public void DeleteMessage(Message message) => 
            context.Messages.Remove(message);

        public async Task<Connection> GetConnectionAsync(string connectionId) =>
            await context.Connections.FindAsync(connectionId);

        public async Task<Group> GetGroupForConnectionAsync(string connectionId) => 
            await context.Groups
                .Include(x => x.Connections)
                .Where(x => x.Connections.Any(y => y.ConnectionId == connectionId))
                .FirstOrDefaultAsync();

        public async Task<Message> GetMessageAsync(int id) =>
            await context.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .FirstOrDefaultAsync(u => u.Id == id);

        public async Task<Group> GetMessageGroupAsync(string groupName) =>
            await context.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);

        public async Task<PagedList<MessageDTO>> GetMessagesForUserAsync(
            MessageParams messageParams)
        {
            var query = context.Messages
                .OrderByDescending(m => m.MessageSent)
                .AsQueryable();

            query = messageParams.Container switch 
            {
                "inbox" => query.Where(u => u.Recipient.UserName == messageParams.UserName &&
                    u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.Sender.UserName == messageParams.UserName &&
                    u.SenderDeleted == false),
                _ => query.Where(u => u.Recipient.UserName == messageParams.UserName && 
                    u.RecipientDeleted == false &&  
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
                    m.RecipientDeleted == false &&
                    m.Sender.UserName == recipientUserName || 
                    m.Recipient.UserName == recipientUserName && 
                    m.Sender.UserName == currentUserName && 
                    m.SenderDeleted == false)
                .OrderBy(m => m.MessageSent)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null &&
                m.Recipient.UserName == currentUserName).ToList();

            if (unreadMessages.Any()) 
                foreach (var message in unreadMessages) 
                    message.DateRead = DateTime.UtcNow;

            return mapper.Map<IEnumerable<MessageDTO>>(messages);
        }

        public void RemoveConnection(Connection connection) =>
            context.Connections.Remove(connection);
    }
}