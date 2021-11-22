using System.Collections.Concurrent;
using UnoGameBackend.Game;

namespace UnoGameBackend.Data;

public class Room
{
    public static ConcurrentBag<Room> Rooms = new() { new Room { Id = 0 } };

    public int Id { get; set; }

    public GamePlay Game { get; set; } = new GamePlay
    {
        Status = GameStatus.Waiting,
    };

    public Player?[] Players { get; set; } = new Player?[8];
    
    /// <summary>
    /// 当前等待出牌的用户
    /// </summary>
    public Player? WaitingForPlay => Players[Game.WaitingForPlayIndex];

    /// <summary>
    /// 下一个出牌用户
    /// </summary>
    public Player NextPlayUser
    {
        get
        {
            var index = this.Game.WaitingForPlayIndex;
            Player? nextUser;
            if (this.Game.PlayOrder == PlayOrder.Clockwise)
            {
                do
                {
                    index += 1;
                    if (index >= this.Players.Length) index = 0;
                    nextUser = this.Players[index];
                } while (nextUser == null);
            }
            else
            {
                do
                {
                    index -= 1;
                    if (index < 0) index = this.Players.Length - 1;
                    nextUser = this.Players[index];
                } while (nextUser == null);
            }

            return nextUser;
        }
    }

    public void GameStart()
    {
        this.Game = new GamePlay { Status = GameStatus.Starting };
        //洗牌
        var deck = Card.GenerateCardsDeck(this.Game.CardDeckNumber, this.Players.Count(p => p != null));
        foreach (var card in deck)
        {
            this.Game.UnusedCards.Enqueue(card);
        }

        //发牌
        foreach (var player in this.Players)
        {
            if (player == null) continue;
            player.HandCards = new(15);
            for (var i = 0; i < 7; i++)
            {
                //起手不能有+4
                player.HandCards.Add(this.Game.UnusedCards.Dequeue());
            }
        }
    }

    public void GameFinish()
    {
        this.Game.Status = GameStatus.Waiting;
        foreach (var player in this.Players)
        {
            if (player == null) continue;
            player.IsReady = false;
        }
    }
}