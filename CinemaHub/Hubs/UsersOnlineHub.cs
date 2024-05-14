using Microsoft.AspNetCore.SignalR;

namespace CinemaHub.Hubs
{
    public class UsersOnlineHub : Hub
    {
        private static HashSet<string> GuestUsers = new HashSet<string>();
        private static HashSet<string> AuthenUsers = new HashSet<string>();
        public override Task OnConnectedAsync()
        {
            //Guest
            if (Context.UserIdentifier == null)
            {
                GuestUsers.Add(Context.ConnectionId);
            } else
            { //Authen
                AuthenUsers.Add(Context.UserIdentifier.ToString());               
            }
            SendUsersCounter();
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //Guest
            if (Context.UserIdentifier == null)
            {
                GuestUsers.Remove(Context.ConnectionId);
            }
            else
            { //Authen
                AuthenUsers.Remove(Context.UserIdentifier.ToString());
            }
            SendUsersCounter();
            return base.OnDisconnectedAsync(exception);
        }

        public void SendUsersCounter()
        {
            int guest = GuestUsers.Count;
            int authen = AuthenUsers.Count;
            Clients.All.SendAsync("GetUsersCounter", new {guest,authen});
        }
    }
}
