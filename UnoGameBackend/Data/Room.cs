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
    /// 当前出牌顺序(默认顺时针)
    /// </summary>
    public PlayOrder PlayOrder { get; set; } = PlayOrder.Clockwise;
    
    /// <summary>
    /// 当前累计的需抽卡数量（+2或+4卡造成）
    /// </summary>
    public int DrawCardActionCount { get; set; }

    public void GameStart()
    {
        this.Game = new GamePlay { Status = GameStatus.Starting };
        this.DrawCardActionCount = 0;
        this.PlayOrder = PlayOrder.Clockwise;
        //洗牌
        var deck = Card.GenerateCardsDeck(this.Game.CardDeckNumber);
        foreach (var card in deck)
        {
            this.Game.UnusedCards.Enqueue(card);
        }

        //发牌
        for (var i = 0; i < 7; i++)
        {
            foreach (var player in this.Players)
            {
                if (player != null)
                {
                    player.HandCards.Add(this.Game.UnusedCards.Dequeue());
                }
            }
        }
    }
}