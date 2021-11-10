using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using UnoGameBackend.Data;

namespace UnoGameBackend.Hubs
{
    public class UnoHub : Hub
    {
        /// <summary>
        /// username与SignalR的connectionId的对应关系
        /// </summary>
        private static ConcurrentDictionary<string, HashSet<string>> UsernameToSignalRId = new();

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //通过connectionId找到用户名
            foreach (var (key, value) in UsernameToSignalRId)
            {
                if (value.Contains(Context.ConnectionId) && Player.Players.ContainsKey(key) &&
                    Player.Players[key] is { } user)
                {
                    //找到该用户所在房间并将其移除
                    foreach (var room in Room.Rooms)
                    {
                        if (room.Players.FirstOrDefault(rp =>
                                rp != null && rp.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)) is
                            { } roomUser)
                        {
                            return LogoutBase(user.Username, room.Id);
                        }
                    }

                    break;
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roomId"></param>
        public async Task UserLogout(string username, int roomId = -1)
        {
            await LogoutBase(username, roomId);
        }

        private async Task LogoutBase(string username, int roomId = -1)
        {
            try
            {
                username = username.Trim();
                if (!Player.Players.ContainsKey(username.ToLower()))
                    throw new Exception("找不到用户");
                var user = Player.Players[username];
                if (roomId >= 0)
                {
                    var room = Room.Rooms.FirstOrDefault(r => r.Id == 0);
                    if (room == null)
                        throw new Exception("找不到房间");

                    var userIndex = Array.IndexOf(room.Players, user);
                    room.Players[userIndex] = null;
                    UsernameToSignalRId.TryRemove(username.ToLower(), out var outValue);

                    await UpdateRoomPlayers(room);
                    await Clients.Group(username.ToLower()).SendAsync("LogoutResult", true, string.Empty);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("LogoutResult", false, e.Message);
            }
        }

        private async Task UpdateRoomPlayers(Room room)
        {
            var info = room.Players.Select(p =>
                p == null
                    ? new { Username = string.Empty, IsReady = false, CardCount = 0 }
                    : new { p.Username, p.IsReady, CardCount = p.HandCards.Count });
            await Clients.Group($"game-{room.Id.ToString()}").SendAsync("UpdateRoomPlayers", info);
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
                if (string.IsNullOrWhiteSpace(username))
                    throw new Exception("昵称不能为空");
                var user = new Player();
                if (Player.Players.ContainsKey(username.ToLower()))
                {
                    user = Player.Players[username.ToLower()];
                }

                user.Username = username;

                Player.Players[username.ToLower()] = user;
                await Groups.AddToGroupAsync(Context.ConnectionId, username.ToLower());
                await Clients.Group(username.ToLower()).SendAsync("LoginResult", true, "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("LoginResult", false, e.Message);
            }
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="username"></param>
        /// <exception cref="Exception"></exception>
        public async Task JoinRoom(int roomId, string username)
        {
            try
            {
                var lowerUsername = username.ToLower();
                if (!Player.Players.ContainsKey(lowerUsername))
                    throw new Exception("找不到用户");
                var user = Player.Players[username.ToLower()];
                var room = Room.Rooms.FirstOrDefault(i => i.Id == roomId);
                if (room == null)
                    throw new Exception("找不到房间");

                if ((room.Game?.Status ?? GameStatus.Waiting) == GameStatus.Playing)
                    throw new Exception("当前房间已开始游戏");

                if (!room.Players.Any(p =>
                        string.Equals(p?.Username, username, StringComparison.CurrentCultureIgnoreCase)))
                {
                    room.Players[Array.IndexOf(room.Players, null)] = user;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{roomId.ToString()}");
                await UpdateRoomPlayers(room);
                await Clients.Group(username.ToLower()).SendAsync("JoinRoomResult", true, "", room.Id);
                var hs = new HashSet<string>();
                if (UsernameToSignalRId.ContainsKey(username))
                {
                    hs = UsernameToSignalRId[username];
                }

                hs.Add(Context.ConnectionId);
                UsernameToSignalRId.AddOrUpdate(username, hs, (k, oldValue) => hs);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("JoinRoomResult", false, e.Message, -1);
            }
        }

        public async Task ToggleReady(string username, int roomId)
        {
            try
            {
                username = username.Trim();
                if (!Player.Players.ContainsKey(username.ToLower()))
                    throw new Exception("找不到用户");
                var user = Player.Players[username.ToLower()];
                user.IsReady = !user.IsReady;
                if (Room.Rooms.FirstOrDefault(r => r.Id == roomId) is { } room)
                {
                    //若所有玩家都已准备
                    if (room.Players.All(p => p?.IsReady ?? true) && room.Players.Count(p => p != null) >= 2)
                    {
                        room.GameStart();
                        await UpdateRoomPlayers(room);
                        await UpdatePlayerHandCards(room);
                        room.Game.Status = GameStatus.Playing;
                        await UpdateGameState(room);
                    }
                    else
                    {
                        room.Game.Status = GameStatus.Waiting;
                        await UpdateGameState(room);
                        await UpdateRoomPlayers(room);
                    }
                }

                await Clients.Group(username.ToLower())
                    .SendAsync("ToggleReadyResult", true, string.Empty, user.IsReady);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("ToggleReadyResult", false, e.Message, false);
            }
        }

        private async Task UpdateGameState(Room room)
        {
            await Clients.Group(room.Id.ToString()).SendAsync("UpdateGameState", room.Game.Status);
        }

        private async Task UpdatePlayerHandCards(Room room)
        {
            foreach (var player in room.Players)
            {
                if (player != null && !string.IsNullOrWhiteSpace(player.Username))
                {
                    await Clients.Group(player.Username.ToLower()).SendAsync("UpdateHandCards",
                        player.HandCards.OrderBy(c => c.CardType).ThenBy(c => c.Color).ThenBy(c => c.CardNumber));
                }
            }
        }
    }
}