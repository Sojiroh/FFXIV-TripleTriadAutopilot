using System;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public static class TriadCards
{
    public const ushort None = ushort.MaxValue;

    public const ushort Hidden = 0;

    public const int MinNumber = 1;

    public const int MaxNumber = 10;

    private static TriadCard[] table = Array.Empty<TriadCard>();

    public static ReadOnlySpan<TriadCard> Table => table;

    public static void Use(TriadCard[] cards)
    {
        table = cards;
    }

    public static ref readonly TriadCard Get(ushort cardId)
    {
        return ref table[cardId];
    }

    public static bool IsPlayable(ushort cardId) => cardId != None && cardId != Hidden && cardId < table.Length && table[cardId].IsValid;

    public static int Number(ushort cardId, int side)
    {
        return table[cardId].Side(side);
    }
}

public enum TriadOwner : byte
{
    Unknown,
    Blue,
    Red
}

public enum TriadGameStatus : byte
{
    InProgressBlue,
    InProgressRed,
    BlueWins,
    BlueDraw,
    BlueLost
}

[Flags]
public enum TriadSpecialRules : byte
{
    None = 0,
    SelectVisible3 = 1,
    SelectVisible5 = 2,
    RandomizeRule = 4,
    RandomizeBlueDeck = 8,
    SwapCards = 0x10,
    BlueCardSelection = 0x20,
    IgnoreOwnedCheck = 0x40
}

public struct TriadBoardSlot
{
    public ushort CardId;

    public TriadOwner Owner;

    public sbyte ScoreModifier;

    public static readonly TriadBoardSlot Empty = new()
    {
        CardId = TriadCards.None
    };

    public readonly bool IsEmpty => CardId == TriadCards.None;

    public readonly TriadCardType Type => TriadCards.Get(CardId).Type;

    public readonly int RawNumber(int side)
    {
        return TriadCards.Number(CardId, side);
    }

    public readonly int Number(int side)
    {
        return Math.Clamp(RawNumber(side) + ScoreModifier, TriadCards.MinNumber, TriadCards.MaxNumber);
    }

    public readonly int OppositeNumber(int side)
    {
        return Number(TriadSide.Opposite(side));
    }
}

public enum TriadDeckState : byte
{
    Valid,
    MissingCards,
    HasDuplicates,
    TooMany4Star,
    TooMany5Star
}
