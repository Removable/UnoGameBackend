namespace UnoGameBackend.Data;

public class Card
{
    public Guid CardId { get; set; } = Guid.NewGuid();
    public CardType CardType { get; set; }

    public int CardNumber { get; set; }

    public CardColor? Color { get; set; }
    
    public bool Selected { get; set; }

    /// <summary>
    /// 生成牌堆
    /// </summary>
    /// <param name="deckNumber"></param>
    /// <param name="playerCount">玩家人数，方便确定起手牌没有+4</param>
    /// <returns></returns>
    public static Card[] GenerateCardsDeck(int deckNumber, int playerCount)
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

        ListRandom(deck, 5);

        //若起手牌有+4，就将其往后放
        var cardCount = playerCount * 7; //每人开局7张牌
        var drawFourCards = deck.ToArray()[..cardCount].Where(c =>
            c.CardType == CardType.UniversalCard && c.CardNumber == (int)CardUniversal.WildDrawFour).ToList();
        if (drawFourCards.Any())
        {
            //从前面移除这些牌
            foreach (var card in drawFourCards)
            {
                deck.Remove(card);
            }
            //放到后面
            foreach (var card in drawFourCards)
            {
                deck.Insert(Random.Shared.Next(cardCount, deck.Count), card);
            }
        }

        return deck.ToArray();
    }

    /// <summary>
    /// 洗牌（打乱List）
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="randomCount"></param>
    /// <typeparam name="T"></typeparam>
    public static void ListRandom<T>(IList<T> sources, int randomCount = 1)
    {
        while (randomCount > 0)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                var index = Random.Shared.Next(0, sources.Count - 1);
                if (index != i)
                {
                    (sources[i], sources[index]) = (sources[index], sources[i]);
                }
            }

            randomCount -= 1;
        }
    }
}