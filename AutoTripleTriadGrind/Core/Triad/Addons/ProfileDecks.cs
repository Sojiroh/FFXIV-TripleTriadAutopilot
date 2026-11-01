using System;
using AutoTripleTriadGrind.Core.Triad.Logic;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class ProfileDecks
{
    public const int SlotCount = 5;

    public const string DeckName = "Triad Grind";

    public unsafe static bool TryRead(int slot, Span<ushort> cards)
    {
        var module = GoldSaucerModule.Instance();
        if (module == null || (uint)slot >= SlotCount)
        {
            return false;
        }
        var deck = module->GetDeck(slot);
        if (deck == null)
        {
            return false;
        }
        Span<ushort> deckCards = deck->Cards;
        for (var i = 0; i < 5; i++)
        {
            cards[i] = deckCards[i];
        }
        return true;
    }

    public static bool HasCards(int slot)
    {
        Span<ushort> cards = stackalloc ushort[5];
        if (!TryRead(slot, cards))
        {
            return false;
        }
        for (var i = 0; i < cards.Length; i++)
        {
            if (!TriadCards.IsPlayable(cards[i]))
            {
                return false;
            }
        }
        return true;
    }

    public unsafe static bool TryWrite(int slot, ReadOnlySpan<ushort> cards)
    {
        var module = GoldSaucerModule.Instance();
        if (module == null || (uint)slot >= SlotCount || cards.Length != 5)
        {
            return false;
        }
        for (var i = 0; i < cards.Length; i++)
        {
            if (!TriadCards.IsPlayable(cards[i]))
            {
                return false;
            }
        }
        var deck = module->GetDeck(slot);
        if (deck != null && ((ReadOnlySpan<ushort>)deck->Cards).SequenceEqual(cards) && deck->NameString == DeckName)
        {
            return true;
        }
        for (var j = 0; j < cards.Length; j++)
        {
            module->SetDeckCard(slot, j, cards[j]);
        }
        module->SetDeckName(slot, DeckName);
        return true;
    }
}
