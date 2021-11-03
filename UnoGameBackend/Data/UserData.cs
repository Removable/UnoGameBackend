using System.Collections.Concurrent;

namespace UnoGameBackend.Data;

public class Player
{
    public static ConcurrentDictionary<string, Player> Players = new();

    public string Username { get; set; } = string.Empty;

    public bool IsReady { get; set; }

    public List<Card> HandCards { get; set; } = new(15);
}