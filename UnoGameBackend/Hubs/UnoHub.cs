using System.Collections.Concurrent;
using System.Timers;
using Microsoft.AspNetCore.SignalR;
using UnoGameBackend.Data;
using UnoGameBackend.Game;
using Timer = System.Timers.Timer;

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

        private async Task SendMessageToUser(string username, string msg)
        {
            await Clients.Group(username.ToLower()).SendAsync("SendMsg", msg);
        }

        private async Task SendMessageToRoom(int roomId, string msg)
        {
            await Clients.Group($"game-{roomId.ToString()}").SendAsync("SendMsg", msg);
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
                var user = Player.Players[username.ToLower()];
                user.IsReady = false;
                await Clients.Group(username.ToLower())
                    .SendAsync("ReadyChanged", true, string.Empty, user.IsReady);
                if (roomId >= 0)
                {
                    var room = Room.Rooms.FirstOrDefault(r => r.Id == 0);
                    if (room == null)
                        throw new Exception("找不到房间");

                    var userIndex = Array.IndexOf(room.Players, user);
                    room.Players[userIndex] = null;
                    UsernameToSignalRId.TryRemove(username.ToLower(), out var outValue);

                    if (room.Players.Count(p => p != null) <= 1)
                    {
                        room.GameFinish();
                        await UpdateGameState(room);
                    }
                    else
                    {
                        room.Game.UsedCards.AddRange(user.HandCards);
                        user.HandCards.Clear();
                    }

                    await UpdateRoomPlayers(room);
                    await Clients.Group(username.ToLower()).SendAsync("LogoutResult", true, string.Empty);
                    foreach (var player in room.Players)
                    {
                        if (player == null) continue;
                        await Clients.Group(player.Username.ToLower())
                            .SendAsync("ReadyChanged", true, string.Empty, player.IsReady);
                    }
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

        /// <summary>
        /// 点击准备
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roomId"></param>
        /// <exception cref="Exception"></exception>
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
                        await UpdateAllPlayersHandCards(room);
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
                    .SendAsync("ReadyChanged", true, string.Empty, user.IsReady);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("ReadyChanged", false, e.Message, false);
            }
        }

        private async Task UpdateGameState(Room room)
        {
            await Clients.Group($"game-{room.Id.ToString()}").SendAsync("UpdateGameState", (int)room.Game.Status,
                room.Game.DrawCardActionCount, room.Game.LastCard.color ?? CardColor.Undefined,
                room.Game.LastCard.card ?? new Card { CardId = Guid.Empty },
                room.Game.WaitingForPlayIndex, room.Game.LastGamePlayAction);
        }

        private async Task UpdateAllPlayersHandCards(Room room)
        {
            foreach (var player in room.Players)
            {
                if (player != null && !string.IsNullOrWhiteSpace(player.Username))
                {
                    await UpdatePlayerHandCards(player);
                }
            }
        }

        private async Task UpdatePlayerHandCards(Player player, bool autoPlay = false)
        {
            await Clients.Group(player.Username.ToLower()).SendAsync("UpdateHandCards",
                player.HandCards.OrderBy(c => c.Color).ThenBy(c => c.CardType).ThenBy(c => c.CardNumber), autoPlay);
        }

        /// <summary>
        /// 判断出牌是否合法
        /// </summary>
        /// <param name="room"></param>
        /// <param name="playCard"></param>
        /// <returns></returns>
        private static (bool success, string msg) JudgePlayRules(Room room, Card playCard)
        {
            var msg = string.Empty;
            var canPlay = false;
            if (room.Game.LastCard.card == null) canPlay = true;
            //如果上一张牌是+2或+4牌，并且累计抽卡数大于0时，则跟牌必须为+2或+4；若累计抽卡数等于0，则表明已完成抽卡，继续正常判断
            else if ((room.Game.LastCard.card.CardType == CardType.ActionCard &&
                      room.Game.LastCard.card.CardNumber == (int)CardAction.DrawTwo ||
                      room.Game.LastCard.card.CardType == CardType.UniversalCard &&
                      room.Game.LastCard.card.CardNumber == (int)CardUniversal.WildDrawFour) &&
                     room.Game.DrawCardActionCount > 0)
            {
                if (playCard.CardType == CardType.ActionCard && playCard.CardNumber == (int)CardAction.DrawTwo ||
                    playCard.CardType == CardType.UniversalCard &&
                    playCard.CardNumber == (int)CardUniversal.WildDrawFour)
                    canPlay = true;
            }
            //万能牌
            else if (playCard.CardType == CardType.UniversalCard)
            {
                canPlay = true;
            }
            //颜色相同
            else if (room.Game.LastCard.color == playCard.Color)
            {
                canPlay = true;
            }
            //卡面相同
            else if (room.Game.LastCard.card.CardType == playCard.CardType &&
                     room.Game.LastCard.card.CardNumber == playCard.CardNumber)
            {
                canPlay = true;
            }

            if (!canPlay)
                msg = "出牌错误~";

            return (canPlay, msg);
        }

        /// <summary>
        /// 出牌
        /// </summary>
        /// <param name="cardId"></param>
        /// <param name="username"></param>
        /// <param name="color"></param>
        public async Task PlayCard(Guid cardId, string username, int color)
        {
            try
            {
                var user = Player.Players[username.ToLower()];
                if (user == null)
                {
                    throw new Exception("数据出错！");
                }

                var room = Room.Rooms.FirstOrDefault(r => r.Players.Contains(user));
                if (room == null || room.Game.Status != GameStatus.Playing)
                {
                    throw new Exception("游戏尚未开始！");
                }

                if (room.WaitingForPlay != user)
                {
                    throw new Exception("未轮到你出牌");
                }

                //要出的牌
                var card = room.Game.UnusedCards.FirstOrDefault(c => c.CardId == cardId);
                if (card != null) throw new Exception("这张牌已被打出！");
                card = user.HandCards.FirstOrDefault(c => c.CardId == cardId);
                if (card == null) throw new Exception("数据错误！");

                //判断出牌是否符合规则
                var (canplay, msg) = JudgePlayRules(room, card);
                if (!canplay) throw new Exception(msg);

                //处理出牌影响
                var interval = 1; //下一位出牌玩家索引偏移量
                var playOrder = room.Game.PlayOrder; //当前出牌顺序
                var drawCardActionCount = room.Game.DrawCardActionCount; //累计需抽牌数
                var gameColor = card.Color;
                switch (card.CardType)
                {
                    case CardType.NumberCard:
                        break;
                    case CardType.ActionCard:
                        switch (card.CardNumber)
                        {
                            default:
                            case (int)CardAction.Skip:
                                interval += 1;
                                break;
                            case (int)CardAction.Reverse:
                                playOrder = playOrder == PlayOrder.Anticlockwise
                                    ? PlayOrder.Clockwise
                                    : PlayOrder.Anticlockwise;
                                break;
                            case (int)CardAction.DrawTwo:
                                drawCardActionCount += 2;
                                break;
                        }

                        break;
                    case CardType.UniversalCard:
                        if (color < 0) throw new Exception("颜色选择错误！");
                        gameColor = (CardColor)color;
                        switch (card.CardNumber)
                        {
                            default:
                            case (int)CardUniversal.Wild:
                                break;
                            case (int)CardUniversal.WildDrawFour:
                                drawCardActionCount += 4;
                                break;
                        }

                        break;
                    default:
                        throw new Exception("出牌错误~");
                }

                card.Selected = false;
                user.HandCards.Remove(card);
                room.Game.LastGamePlayAction = GamePlayAction.Play;

                //仅余一张卡，且非数字卡时，需要自动补一张
                if (user.HandCards.Count == 1 && user.HandCards.FirstOrDefault()!.CardType != CardType.NumberCard)
                {
                    user.HandCards.AddRange(room.Game.DrawCard(1));
                }

                //牌出完，游戏结束
                if (user.HandCards.Count <= 0)
                {
                    room.GameFinish();

                    await SendMessageToRoom(room.Id, $"游戏结束！获胜者是：{username}");
                    return;
                }

                room.Game.UsedCards.Add(card);
                room.Game.LastCard = (card, gameColor);
                room.Game.PlayOrder = playOrder;
                //找到下一位应该出牌的玩家
                var currentIndex = room.Game.WaitingForPlayIndex;
                while (interval > 0)
                {
                    CalcPlayOrderIndex(room, ref currentIndex);
                    interval -= 1;
                    room.Game.WaitingForPlayIndex = currentIndex;
                }

                room.Game.DrawCardActionCount = drawCardActionCount;
                await Clients.Group($"game-{room.Id.ToString()}").SendAsync("PlayCardResult", true, "",
                    new { card, index = Array.IndexOf(room.Players, user), lastGameAction = GamePlayAction.Play });
                await UpdatePlayerHandCards(user);
                await UpdateRoomPlayers(room);
                await UpdateGameState(room);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("PlayCardResult", false, e.Message, null);
            }
        }

        /// <summary>
        /// 抽牌
        /// </summary>
        /// <param name="username"></param>
        /// <exception cref="Exception"></exception>
        public async Task DrawCard(string username)
        {
            try
            {
                var user = Player.Players[username.ToLower()];
                if (user == null)
                {
                    throw new Exception("数据出错！");
                }

                var room = Room.Rooms.FirstOrDefault(r => r.Players.Contains(user));
                if (room == null || room.Game.Status != GameStatus.Playing)
                {
                    throw new Exception("数据出错！");
                }

                if (room.WaitingForPlay != user)
                {
                    throw new Exception("未轮到你抽牌");
                }

                var drawCount = room.Game.DrawCardActionCount;
                drawCount = drawCount == 0 ? 1 : drawCount;

                //抽卡
                if (drawCount == 1)
                {
                    var shouldPlayCard = room.Game.UnusedCards.FirstOrDefault();
                    if (shouldPlayCard != null)
                    {
                        //在只抽一张卡的情况下，若抽到的卡符合出牌标准，应直接出牌
                        var (canplay, msg) = JudgePlayRules(room, shouldPlayCard);
                        if (canplay)
                        {
                            shouldPlayCard = room.Game.DrawCard(1)[0];
                            user.HandCards.Add(shouldPlayCard);
                            shouldPlayCard.Selected = true;
                            //无需改变颜色时，直接打出
                            await UpdatePlayerHandCards(user, autoPlay: true);
                            if (shouldPlayCard.CardType != CardType.UniversalCard)
                            {
                                // await PlayCard(shouldPlayCard.CardId, username, -1);
                                await SendMessageToUser(username, "抽到的牌已直接打出");
                            }

                            return;
                        }
                    }
                }

                var drawnCard = room.Game.DrawCard(drawCount);

                user.HandCards.AddRange(drawnCard);
                room.Game.DrawCardActionCount = 0;
                //设置下一个抽卡玩家
                var orderIndex = room.Game.WaitingForPlayIndex;
                CalcPlayOrderIndex(room, ref orderIndex);

                room.Game.WaitingForPlayIndex = orderIndex;
                room.Game.LastGamePlayAction = GamePlayAction.Draw;

                await UpdatePlayerHandCards(user);
                await UpdateRoomPlayers(room);
                await UpdateGameState(room);
                await Clients.Group($"game-{room.Id.ToString()}").SendAsync("PlayCardResult", true, "",
                    new
                    {
                        card = new Card { CardId = Guid.Empty }, index = Array.IndexOf(room.Players, user),
                        lastGameAction = GamePlayAction.Draw
                    });
                await Clients.Group(username.ToLower()).SendAsync("DrawCardResult", true, "", null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Clients.Group(username.ToLower()).SendAsync("DrawCardResult", false, e.Message, null);
            }
        }

        /// <summary>
        /// 计算下个出牌玩家的索引
        /// </summary>
        /// <param name="room"></param>
        /// <param name="orderIndex"></param>
        private void CalcPlayOrderIndex(Room room, ref int orderIndex)
        {
            do
            {
                if (room.Game.PlayOrder == PlayOrder.Clockwise)
                {
                    orderIndex += 1;
                    if (orderIndex >= room.Players.Length)
                        orderIndex = 0;
                }
                else
                {
                    orderIndex -= 1;
                    if (orderIndex < 0)
                        orderIndex = room.Players.Length - 1;
                }
            } while (room.Players[orderIndex] == null);
        }
    }
}