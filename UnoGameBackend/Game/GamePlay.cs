using UnoGameBackend.Data;

namespace UnoGameBackend.Game;

public class GamePlay
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public GameStatus Status { get; set; }

    /// <summary>
    /// 卡组数量
    /// </summary>
    public int CardDeckNumber { get; set; } = 1;

    public List<Card> UsedCards { get; set; } = new(108);

    public Queue<Card> UnusedCards { get; set; } = new(108);

    /// <summary>
    /// 上一张牌
    /// </summary>
    public (Card? card, CardColor? color) LastCard { get; set; }

    /// <summary>
    /// 当前出牌顺序(默认顺时针)
    /// </summary>
    public PlayOrder PlayOrder { get; set; } = PlayOrder.Clockwise;

    /// <summary>
    /// 当前等待出牌的用户索引
    /// </summary>
    public int WaitingForPlayIndex { get; set; }

    /// <summary>
    /// 当前累计的需抽卡数量（+2或+4卡造成）
    /// </summary>
    public int DrawCardActionCount { get; set; }

    /// <summary>
    /// 洗牌
    /// </summary>
    private void Shuffle()
    {
        Card.ListRandom(UsedCards, 5);
        foreach (var gameUsedCard in UsedCards)
        {
            UnusedCards.Enqueue(gameUsedCard);
        }

        UsedCards.Clear();
    }

    /// <summary>
    /// 抽牌
    /// </summary>
    /// <returns></returns>
    public IList<Card> DrawCard(int drawCount)
    {
        if (UnusedCards.Count < drawCount)
        {
            Shuffle();
        }

        var drawCards = new List<Card>();
        for (var i = 0; i < drawCount; i++)
        {
            drawCards.Add(UnusedCards.Dequeue());
        }

        return drawCards;
    }
}