using Microsoft.AspNetCore.SignalR;
using UnoGameBackend.Data;

namespace UnoGameBackend.Hubs
{
    public class UnoHub : Hub
    {
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username"></param>
        public async Task UserLogin(string username)
        {
            try
            {
                username = username.Trim();
                var user = new Player();
                if (Player.Players.ContainsKey(username.ToLower()))
                {
                    user = Player.Players[username.ToLower()];
                }

                user.Username = username;

                Player.Players[username.ToLower()] = user;
                await Groups.AddToGroupAsync(Context.ConnectionId, username.ToLower());
                await Clients.Group(username.ToLower()).SendAsync("LoginResult", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("LoginResult", false);
            }
        }
    }
}