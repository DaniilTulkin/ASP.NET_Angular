using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly PresenceTracker presenceTracker;
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        public MessageHub(IMapper mapper,  IHubContext<PresenceHub> presenceHub,
            PresenceTracker presenceTracker, IUnitOfWork unitOfWork)
        {
            this.presenceHub = presenceHub;
            this.presenceTracker = presenceTracker;
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public override async Task OnConnectedAsync() 
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroupAsync(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = unitOfWork.MessageRepository
                .GetMessageThreadAsync(Context.User.GetUserName(), otherUser).Result;
            if (unitOfWork.HasChanges()) await unitOfWork.CompleteAsync();

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception ex) 
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);

            await base.OnDisconnectedAsync(ex);
        }

        public async Task SendMessageAsync(CreateMessageDTO createMessageDTO) 
        {
            var userName = Context.User.GetUserName();
            if (userName == createMessageDTO.RecipientUserName.ToLower())
                throw new HubException("You cannot send messages to yourself");

            var sender = await unitOfWork.UserRepository.GetUserByNameAsync(userName);
            var recipient = await unitOfWork.UserRepository.GetUserByNameAsync(createMessageDTO.RecipientUserName);
            if (recipient == null) throw new HubException("Not found user");

            var message = new Message 
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUserName = recipient.UserName,
                Content = createMessageDTO.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);

            if (group.Connections.Any(x => x.UserName == recipient.UserName)) 
            {
                message.DateRead = DateTime.UtcNow;
            }
            else 
            {
                var connections = await presenceTracker.GetConnectionsForUserAsync(recipient.UserName);
                if (connections != null) 
                {
                    await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                        new {userName = sender.UserName, knownAs = sender.KnownAs});
                }
            }

            unitOfWork.MessageRepository.AddMessage(message);
            if (await unitOfWork.CompleteAsync()) 
            {
                await Clients.Group(groupName).SendAsync(
                    "NewMessage", mapper.Map<MessageDTO>(message));
            }
        }

        private async Task<Group> AddToGroupAsync(string groupName)
        {
            var group = await unitOfWork.MessageRepository.GetMessageGroupAsync(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());

            if (group == null) 
            {
                group = new Group(groupName);
                unitOfWork.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);
            if (await unitOfWork.CompleteAsync()) return group;
            throw new HubException("Failed to join group");
        }

        private async Task<Group> RemoveFromMessageGroup() 
        {
            var group = await unitOfWork.MessageRepository.GetGroupForConnectionAsync(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await unitOfWork.CompleteAsync()) return group;
            throw new HubException("Failed to remove from group");
        }

        private string GetGroupName(string caller, string other) 
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}