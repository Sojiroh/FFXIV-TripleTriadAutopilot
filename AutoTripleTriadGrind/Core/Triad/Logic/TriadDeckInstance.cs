namespace AutoTripleTriadGrind.Core.Triad.Logic;

public abstract class TriadDeckInstance
{
    public const int MaxAvailableCards = 15;

    public const int MaxCardsToPlace = 5;

    public int AvailableCardMask;

    public TriadDeck? Deck;

    public int NumPlaced;

    public int NumUnknownPlaced;

    public abstract void OnCardPlaced(int cardIndex);

    public abstract int FirstAvailableCard();

    public abstract ushort GetCard(int cardIndex);

    public abstract int GetCardIndex(ushort cardId);

    public abstract TriadDeckInstance CreateCopy();

    public bool IsPlaced(int cardIndex)
    {
        return (AvailableCardMask & (1 << cardIndex)) == 0;
    }

    public ushort FirstAvailableCardId()
    {
        var cardIndex = FirstAvailableCard();
        if (cardIndex >= 0)
        {
            return GetCard(cardIndex);
        }
        return TriadCards.None;
    }
}
