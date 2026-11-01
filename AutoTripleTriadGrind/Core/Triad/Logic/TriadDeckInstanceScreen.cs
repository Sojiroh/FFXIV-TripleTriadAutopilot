using System;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadDeckInstanceScreen : TriadDeckInstance
{
    public ushort[] Cards = NewHand();

    public ushort SwappedCard = TriadCards.None;

    public int SwappedCardIndex = -1;

    public int UnknownPoolMask;

    public TriadDeckInstanceScreen()
    {
    }

    private TriadDeckInstanceScreen(TriadDeckInstanceScreen copyFrom)
    {
        Cards = (ushort[])copyFrom.Cards.Clone();
        Deck = copyFrom.Deck;
        NumUnknownPlaced = copyFrom.NumUnknownPlaced;
        NumPlaced = copyFrom.NumPlaced;
        AvailableCardMask = copyFrom.AvailableCardMask;
        UnknownPoolMask = copyFrom.UnknownPoolMask;
        SwappedCardIndex = copyFrom.SwappedCardIndex;
        SwappedCard = copyFrom.SwappedCard;
    }

    public static ushort[] NewHand()
    {
        var hand = new ushort[5];
        Array.Fill(hand, TriadCards.None);
        return hand;
    }

    public override TriadDeckInstance CreateCopy()
    {
        return new TriadDeckInstanceScreen(this);
    }

    public override int FirstAvailableCard()
    {
        for (var i = 0; i < Cards.Length; i++)
        {
            if ((AvailableCardMask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1;
    }

    public void UpdateAvailableCards(ReadOnlySpan<ushort> screenCards)
    {
        AvailableCardMask = 0;
        NumPlaced = 0;
        NumUnknownPlaced = 0;
        screenCards.Slice(0, Cards.Length).CopyTo(Cards);
        for (var i = 0; i < Cards.Length; i++)
        {
            var card = Cards[i];
            if (card == TriadCards.None)
            {
                NumPlaced++;
            }
            else if (card != TriadCards.Hidden)
            {
                AvailableCardMask |= 1 << i;
            }
        }
    }

    public void SetSwappedCard(ushort swappedCard, int swappedCardIndex)
    {
        SwappedCard = swappedCard;
        SwappedCardIndex = swappedCardIndex;
        if (swappedCardIndex >= 0)
        {
            UnknownPoolMask &= ~(1 << swappedCardIndex);
        }
    }

    public override void OnCardPlaced(int cardIndex)
    {
        var cardBit = 1 << cardIndex;
        AvailableCardMask &= ~cardBit;
        if (Deck == null || (UnknownPoolMask & cardBit) == 0)
        {
            return;
        }
        NumUnknownPlaced++;
        if (NumUnknownPlaced >= Cards.Length - Deck.KnownCards.Count)
        {
            AvailableCardMask &= ~UnknownPoolMask;
        }
    }

    public override ushort GetCard(int cardIndex)
    {
        if (cardIndex < 0)
        {
            return TriadCards.None;
        }
        if (cardIndex == SwappedCardIndex)
        {
            return SwappedCard;
        }
        if (cardIndex < Cards.Length)
        {
            return Cards[cardIndex];
        }
        return Deck?.GetCard(cardIndex - Cards.Length) ?? TriadCards.None;
    }

    public override int GetCardIndex(ushort cardId)
    {
        if (cardId == TriadCards.None)
        {
            return -1;
        }
        if (cardId == SwappedCard)
        {
            return SwappedCardIndex;
        }
        var index = Array.IndexOf(Cards, cardId);
        if (index >= 0 || Deck == null)
        {
            return index;
        }
        index = Deck.GetCardIndex(cardId);
        if (index < 0)
        {
            return -1;
        }
        return index + Cards.Length;
    }
}

public readonly record struct TriadOptimizedDeck(ushort[] Cards, float WinChance, long TestedDecks, long PossibleDecks);
