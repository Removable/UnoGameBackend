namespace UnoGameBackend.Data;

/// <summary>
/// 游戏状态
/// </summary>
public enum GameStatus
{
    Waiting,
    Starting,
    Playing,
    Finished,
}

/// <summary>
/// uno卡类型
/// </summary>
public enum CardType
{
    NumberCard = 1,
    ActionCard = 2,
    UniversalCard = 3,
}

/// <summary>
/// 颜色
/// </summary>
public enum CardColor
{
    Undefined = -1,
    Red = 0,
    Yellow = 1,
    Blue = 2,
    Green =3,
}

/// <summary>
/// 功能牌类型
/// </summary>
public enum CardAction
{
    Skip = 1,
    Reverse = 2,
    DrawTwo = 3,
}

/// <summary>
/// 万能牌类型
/// </summary>
public enum CardUniversal
{
    Wild = 1,
    WildDrawFour = 2,
}

/// <summary>
/// 出牌顺序
/// </summary>
public enum PlayOrder
{
    /// <summary>
    /// 顺时针（正序）
    /// </summary>
    Clockwise,
    /// <summary>
    /// 逆时针（倒叙）
    /// </summary>
    Anticlockwise,
}

public enum GamePlayAction
{
    /// <summary>
    /// 出牌
    /// </summary>
    Play,
    /// <summary>
    /// 抽卡
    /// </summary>
    Draw,
}