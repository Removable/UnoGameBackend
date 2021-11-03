namespace UnoGameBackend.Data;

public class Card
{
    public CardType CardType { get; set; }

    public int CardNumber { get; set; }

    public CardColor? Color { get; set; }

    /// <summary>
    /// 生成牌堆
    /// </summary>
    /// <param name="deckNumber"></param>
    /// <returns></returns>
    public static Card[] GenerateCardsDeck(int deckNumber)
    {
        var deck = new List<Card>(108 * deckNumber);

        #region 向牌堆加牌

        //万能牌，每副牌每种4张
        foreach (int enumValue in Enum.GetValues(typeof(CardUniversal)))
        {
            for (var i = 0; i < deckNumber * 4; i++)
            {
                deck.Add(new Card
                {
                    CardNumber = enumValue,
                    CardType = CardType.UniversalCard,
                });
            }
        }

        //四种颜色
        foreach (int colorValue in Enum.GetValues(typeof(CardColor)))
        {
            //功能牌，每副牌每种2张

            for (var i = 0; i < deckNumber * 2; i++)
            {
                foreach (int actionValue in Enum.GetValues(typeof(CardAction)))
                {
                    deck.Add(new Card
                    {
                        CardNumber = actionValue,
                        CardType = CardType.ActionCard,
                        Color = (CardColor)colorValue,
                    });
                }
            }

            //数字牌，每副牌一张0，其他数字每种两张
            for (var i = 0; i < deckNumber * 1; i++)
            {
                deck.Add(new Card
                {
                    CardNumber = 0,
                    CardType = CardType.NumberCard,
                    Color = (CardColor)colorValue,
                });
            }

            for (var number = 1; number < 10; number++)
            {
                for (var i = 0; i < deckNumber * 2; i++)
                {
                    deck.Add(new Card
                    {
                        CardNumber = number,
                        CardType = CardType.NumberCard,
                        Color = (CardColor)colorValue,
                    });
                }
            }
        }

        #endregion

        ListRandom(deck);

        return deck.ToArray();
    }

    /// <summary>
    /// 洗牌（打乱List）
    /// </summary>
    /// <param name="sources"></param>
    /// <typeparam name="T"></typeparam>
    public static void ListRandom<T>(IList<T> sources)
    {
        var rd = new Random(Guid.NewGuid().GetHashCode());
        for (var i = 0; i < sources.Count; i++)
        {
            var index = rd.Next(0, sources.Count - 1);
            if (index != i)
            {
                (sources[i], sources[index]) = (sources[index], sources[i]);
            }
        }
    }
}