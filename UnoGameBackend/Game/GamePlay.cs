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
}