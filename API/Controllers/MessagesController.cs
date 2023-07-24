using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IMapper mapper;

        public MessagesController(IUserRepository userRepository,
            IMessageRepository messageRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.messageRepository = messageRepository;
            this.userRepository = userRepository;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessageAsync(
            CreateMessageDTO createMessageDTO) 
        {
            var userName = User.GetUserName();
            if (userName == createMessageDTO.RecipientUserName.ToLower())
                return BadRequest("You cannot send messages to yourself");

            var sender = await userRepository.GetUserByNameAsync(userName);
            var recipient = await userRepository.GetUserByNameAsync(createMessageDTO.RecipientUserName);
            if (recipient == null) return NotFound();

            var message = new Message 
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUserName = recipient.UserName,
                Content = createMessageDTO.Content
            };

            messageRepository.AddMessage(message);
            if (await messageRepository.SaveAllAsync()) 
                return Ok(mapper.Map<MessageDTO>(message));
            
            return BadRequest("Failed to send message");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser(
            [FromQuery] MessageParams messageParams
        )
        {
            messageParams.UserName = User.GetUserName();

            var messages = await messageRepository.GetMessagesForUserAsync(messageParams);
             Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                messages.TotalCount, messages.TotalPages);
            
            return messages;
        }

        [HttpGet("thread/{userName}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThreadAsync(
            string userName) 
        {
            var currentUserName = User.GetUserName();

            return Ok(await messageRepository.GetMessageThreadAsync(currentUserName,
                userName));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessageAsync(int id) 
        {
            var userName = User.GetUserName();
            var message = await messageRepository.GetMessageAsync(id);

            if (message.Sender.UserName != userName && 
                message.Recipient.UserName != userName) 
                return Unauthorized();

            if (message.Sender.UserName == userName)
                message.SenderDeleted = true;

            if (message.Recipient.UserName == userName)
                message.RecipientDeleted = true;

            if (message.SenderDeleted && message.RecipientDeleted)
                messageRepository.DeleteMessage(message);

            if (await messageRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Problem deleting the message");
        }
    }
}