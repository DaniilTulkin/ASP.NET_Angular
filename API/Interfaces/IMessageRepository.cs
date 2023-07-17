using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IMessageRepository
    {
        void AddMessage(Message message);
        void DeleteMessage(Message message);
        Task<Message> GetMessageAsync(int id);
        Task<PagedList<MessageDTO>> GetMessagesForUserAsync(MessageParams messageParams);
        Task<IEnumerable<MessageDTO>> GetMessageThreadAsync(
            string currentUserName, string recipientUserName);
        Task<bool> SaveAllAsync();
    }
}