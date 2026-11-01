namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadDeckInstanceManual : TriadDeckInstance
{
    private readonly TriadDeck deck;

    public TriadDeck Source => deck;

    public TriadDeckInstanceManual(TriadDeck deck)
    {
        this.deck = deck;
        Deck = deck;
        AvailableCardMask = (1 << deck.CardCount) - 1;
    }

    private TriadDeckInstanceManual(TriadDeckInstanceManual copyFrom)
    {
        deck = copyFrom.deck;
        Deck = copyFrom.deck;
        NumUnknownPlaced = copyFrom.NumUnknownPlaced;
        NumPlaced = copyFrom.NumPlaced;
        AvailableCardMask = copyFrom.AvailableCardMask;
    }

    public override TriadDeckInstance CreateCopy()
    {
        return new TriadDeckInstanceManual(this);
    }

    public override void OnCardPlaced(int cardIndex)
    {
        AvailableCardMask &= ~(1 << cardIndex);
        NumPlaced++;
        var count = deck.KnownCards.Count;
        if (cardIndex < count)
        {
            return;
        }
        NumUnknownPlaced++;
        if (NumUnknownPlaced >= MaxCardsToPlace - count)
        {
            AvailableCardMask &= (1 << count) - 1;
        }
    }

    public override int FirstAvailableCard()
    {
        var count = deck.KnownCards.Count;
        for (var i = 0; i < count; i++)
        {
            if ((AvailableCardMask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1;
    }

    public override ushort GetCard(int cardIndex)
    {
        return deck.GetCard(cardIndex);
    }

    public override int GetCardIndex(ushort cardId)
    {
        var knownIndex = deck.KnownCards.IndexOf(cardId);
        if (knownIndex < 0)
        {
            return deck.UnknownPool.IndexOf(cardId) + deck.KnownCards.Count;
        }
        return knownIndex;
    }
}
