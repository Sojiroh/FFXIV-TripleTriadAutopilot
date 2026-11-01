using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadDeck
{
    public const int HandSize = 5;

    public readonly List<ushort> KnownCards;

    public readonly List<ushort> UnknownPool;

    public int CardCount => KnownCards.Count + UnknownPool.Count;

    public TriadDeck()
    {
        KnownCards = new(HandSize);
        UnknownPool = new();
    }

    public TriadDeck(IReadOnlyList<ushort> knownCards, IReadOnlyList<ushort>? unknownPool = null)
    {
        KnownCards = new(knownCards.Count);
        AddPlayable(KnownCards, knownCards);
        UnknownPool = new(unknownPool?.Count ?? 0);
        if (unknownPool != null)
        {
            AddPlayable(UnknownPool, unknownPool);
        }
    }

    public TriadDeck(ReadOnlySpan<ushort> knownCards, ReadOnlySpan<ushort> unknownPool)
    {
        KnownCards = new(knownCards.Length);
        for (var i = 0; i < knownCards.Length; i++)
        {
            if (TriadCards.IsPlayable(knownCards[i]))
            {
                KnownCards.Add(knownCards[i]);
            }
        }
        UnknownPool = new(unknownPool.Length);
        for (var j = 0; j < unknownPool.Length; j++)
        {
            if (TriadCards.IsPlayable(unknownPool[j]))
            {
                UnknownPool.Add(unknownPool[j]);
            }
        }
    }

    public ushort GetCard(int index)
    {
        if (index < 0)
        {
            return TriadCards.None;
        }
        if (index < KnownCards.Count)
        {
            return KnownCards[index];
        }
        if (index >= CardCount)
        {
            return TriadCards.None;
        }
        return UnknownPool[index - KnownCards.Count];
    }

    public int GetCardIndex(ushort cardId)
    {
        var index = KnownCards.IndexOf(cardId);
        if (index >= 0)
        {
            return index;
        }
        index = UnknownPool.IndexOf(cardId);
        if (index < 0)
        {
            return -1;
        }
        return index + KnownCards.Count;
    }

    public bool SetCard(int index, ushort cardId)
    {
        if (index < 0)
        {
            return false;
        }
        if (index < KnownCards.Count)
        {
            KnownCards[index] = cardId;
            return true;
        }
        if (index >= CardCount)
        {
            return false;
        }
        UnknownPool[index - KnownCards.Count] = cardId;
        return true;
    }

    public TriadDeckState Validate(Func<ushort, bool> isOwned)
    {
        var fourStarCount = 0;
        var fiveStarCount = 0;
        for (var i = 0; i < KnownCards.Count; i++)
        {
            var cardId = KnownCards[i];
            if (!isOwned(cardId))
            {
                return TriadDeckState.MissingCards;
            }
            for (var j = 0; j < KnownCards.Count; j++)
            {
                if (j != i && KnownCards[j] == cardId)
                {
                    return TriadDeckState.HasDuplicates;
                }
            }
            var stars = TriadCards.Get(cardId).Stars;
            if (stars == 5)
            {
                fiveStarCount++;
            }
            else if (stars == 4)
            {
                fourStarCount++;
            }
        }
        if (fiveStarCount > 1)
        {
            return TriadDeckState.TooMany5Star;
        }
        if (fourStarCount + fiveStarCount <= 2)
        {
            return TriadDeckState.Valid;
        }
        return TriadDeckState.TooMany4Star;
    }

    public bool SameCards(TriadDeck other) => SameSet(KnownCards, other.KnownCards) && SameSet(UnknownPool, other.UnknownPool);

    public string Describe()
    {
        var builder = new StringBuilder(64);
        for (var i = 0; i < KnownCards.Count; i++)
        {
            builder.Append((i == 0) ? string.Empty : ",").Append(KnownCards[i]);
        }
        if (UnknownPool.Count == 0)
        {
            return builder.ToString();
        }
        builder.Append(" + [");
        for (var j = 0; j < UnknownPool.Count; j++)
        {
            builder.Append((j == 0) ? string.Empty : ",").Append(UnknownPool[j]);
        }
        return builder.Append(']').ToString();
    }

    private static void AddPlayable(List<ushort> target, IReadOnlyList<ushort> source)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (TriadCards.IsPlayable(source[i]))
            {
                target.Add(source[i]);
            }
        }
    }

    private static bool SameSet(List<ushort> left, List<ushort> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        for (var i = 0; i < left.Count; i++)
        {
            if (!right.Contains(left[i]))
            {
                return false;
            }
        }
        return true;
    }
}
