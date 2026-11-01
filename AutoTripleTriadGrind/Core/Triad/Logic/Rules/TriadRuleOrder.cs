using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public sealed class TriadRuleOrder : TriadRule
{
    public TriadRuleOrder()
        : base(TriadRuleId.Order, TriadRuleFeatures.FilterNext, TriadSpecialRules.None, allowsCombo: false, deckOrderMatters: true)
    {
    }

    public override void OnFilterNextCards(TriadGameState state, ref int allowedCardsMask)
    {
        if (state.Status != TriadGameStatus.InProgressBlue || allowedCardsMask == 0)
        {
            return;
        }
        var firstCard = state.DeckBlue.FirstAvailableCard();
        allowedCardsMask = ((firstCard < 0) ? 0 : (1 << firstCard));
    }
}

public sealed class TriadRuleAllOpen : TriadRule
{
    public TriadRuleAllOpen()
        : base(TriadRuleId.AllOpen, TriadRuleFeatures.None, TriadSpecialRules.SelectVisible5)
    {
    }

    public static void MakeKnown(TriadGameState state, IReadOnlyList<int> redIndices)
    {
        if (state.DeckRed is not TriadDeckInstanceManual redManual || redIndices.Count > TriadDeck.HandSize)
        {
            return;
        }
        var source = redManual.Source;
        var revealedDeck = new TriadDeck(source.KnownCards, source.UnknownPool);
        for (var i = 0; i < redIndices.Count; i++)
        {
            var redIndex = redIndices[i];
            if (redIndex >= source.KnownCards.Count)
            {
                var cardId = source.UnknownPool[redIndex - source.KnownCards.Count];
                revealedDeck.KnownCards.Add(cardId);
                revealedDeck.UnknownPool.Remove(cardId);
            }
        }
        for (var j = 0; j < revealedDeck.KnownCards.Count && revealedDeck.KnownCards.Count > TriadDeck.HandSize; j++)
        {
            if (!Contains(redIndices, redManual.GetCardIndex(revealedDeck.KnownCards[j])))
            {
                revealedDeck.KnownCards.RemoveAt(j);
                j--;
            }
        }
        state.DeckRed = new TriadDeckInstanceManual(revealedDeck);
    }

    private static bool Contains(IReadOnlyList<int> values, int value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return true;
            }
        }
        return false;
    }
}

public sealed class TriadRuleSwap : TriadRule
{
    public TriadRuleSwap()
        : base(TriadRuleId.Swap, TriadRuleFeatures.None, TriadSpecialRules.SwapCards)
    {
    }

    public static void SwapCards(TriadGameState state, ushort fromBlue, int blueSlot, ushort fromRed)
    {
        if (state.DeckBlue is not TriadDeckInstanceManual blueManual || state.DeckRed is not TriadDeckInstanceManual redManual)
        {
            return;
        }
        var blueDeck = new TriadDeck(blueManual.Source.KnownCards, blueManual.Source.UnknownPool);
        var redDeck = new TriadDeck(redManual.Source.KnownCards, redManual.Source.UnknownPool);
        redDeck.KnownCards.Add(fromBlue);
        redDeck.KnownCards.Remove(fromRed);
        redDeck.UnknownPool.Remove(fromRed);
        blueDeck.KnownCards[blueSlot] = fromRed;
        state.DeckBlue = new TriadDeckInstanceManual(blueDeck);
        state.DeckRed = new TriadDeckInstanceManual(redDeck);
    }
}

public sealed class TriadRuleSuddenDeath : TriadRule
{
    private const int MaxRestarts = 3;

    public TriadRuleSuddenDeath()
        : base(TriadRuleId.SuddenDeath, TriadRuleFeatures.AllPlaced)
    {
    }

    public override void OnAllCardsPlaced(TriadGameState state)
    {
        if (state.Status != TriadGameStatus.BlueDraw || state.NumRestarts >= MaxRestarts)
        {
            return;
        }
        if (state.DeckBlue is not TriadDeckInstanceManual blueManual || state.DeckRed is not TriadDeckInstanceManual redManual)
        {
            return;
        }
        List<ushort> blueCards = new(TriadDeck.HandSize);
        List<ushort> redCards = new(TriadDeck.HandSize);
        List<ushort> redUnknownPool = new();
        var board = state.Board;
        for (var i = 0; i < board.Length; i++)
        {
            if (board[i].Owner == TriadOwner.Blue)
            {
                blueCards.Add(board[i].CardId);
            }
            else
            {
                redCards.Add(board[i].CardId);
            }
            board[i] = TriadBoardSlot.Empty;
        }
        if (blueManual.NumPlaced < redManual.NumPlaced)
        {
            AddFirstUnplaced(blueManual, blueCards);
            state.Status = TriadGameStatus.InProgressBlue;
        }
        else
        {
            AddFirstUnplaced(redManual, redCards);
            if (redCards.Count < blueCards.Count)
            {
                var source = redManual.Source;
                for (var j = 0; j < source.UnknownPool.Count; j++)
                {
                    if (!redManual.IsPlaced(j + source.KnownCards.Count))
                    {
                        redUnknownPool.Add(source.UnknownPool[j]);
                    }
                }
            }
            state.Status = TriadGameStatus.InProgressRed;
        }
        state.DeckBlue = new TriadDeckInstanceManual(new TriadDeck(blueCards));
        state.DeckRed = new TriadDeckInstanceManual(new TriadDeck(redCards, redUnknownPool));
        state.NumCardsPlaced = 0;
        state.NumRestarts++;
        state.ClearTypeModifiers();
    }

    private static void AddFirstUnplaced(TriadDeckInstanceManual deck, List<ushort> cards)
    {
        var knownCards = deck.Source.KnownCards;
        for (var i = 0; i < knownCards.Count; i++)
        {
            if (!deck.IsPlaced(i))
            {
                cards.Add(knownCards[i]);
                break;
            }
        }
    }
}
