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
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser(
            [FromQuery] MessageParams messageParams
        )
        {
            messageParams.UserName = User.GetUserName();

            var messages = await unitOfWork.MessageRepository.GetMessagesForUserAsync(messageParams);
             Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                messages.TotalCount, messages.TotalPages);
            
            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessageAsync(int id) 
        {
            var userName = User.GetUserName();
            var message = await unitOfWork.MessageRepository.GetMessageAsync(id);

            if (message.Sender.UserName != userName && 
                message.Recipient.UserName != userName) 
                return Unauthorized();

            if (message.Sender.UserName == userName)
                message.SenderDeleted = true;

            if (message.Recipient.UserName == userName)
                message.RecipientDeleted = true;

            if (message.SenderDeleted && message.RecipientDeleted)
                unitOfWork.MessageRepository.DeleteMessage(message);

            if (await unitOfWork.CompleteAsync())
                return Ok();

            return BadRequest("Problem deleting the message");
        }
    }
}