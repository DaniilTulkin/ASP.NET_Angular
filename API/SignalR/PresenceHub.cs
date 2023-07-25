using System;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        public override async Task OnConnectedAsync() 
        {
            await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUserName());
        }

        public override async Task OnDisconnectedAsync(Exception ex) 
        {
            await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUserName());

            await base.OnDisconnectedAsync(ex);
        }
    }
}