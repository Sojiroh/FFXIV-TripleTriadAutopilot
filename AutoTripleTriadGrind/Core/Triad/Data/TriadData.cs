using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Logic;

namespace AutoTripleTriadGrind.Core.Triad.Data;

internal static class TriadData
{
    public const ushort NoNpc = ushort.MaxValue;

    private static TriadDataSet? current;

    public static bool Loaded => current is not null;

    public static string FailureReason { get; private set; } = string.Empty;

    public static TriadDataSet Set => current ?? TriadDataSet.Empty;

    public static event Action? DataLoaded;

    public static void EnsureLoaded()
    {
        if (current != null || FailureReason.Length > 0)
        {
            return;
        }
        if (TriadDataLoader.TryLoad(out var loaded, out var failure))
        {
            current = loaded;
            TriadCards.Use(loaded.Cards);
            DataLoaded?.Invoke();
        }
        else
        {
            FailureReason = failure;
        }
    }
}

internal sealed class TriadDataSet
{
    public static readonly TriadDataSet Empty = new();

    public TriadCard[] Cards { get; init; } = Array.Empty<TriadCard>();

    public string[] CardNames { get; init; } = Array.Empty<string>();

    public uint[] CardItemIds { get; init; } = Array.Empty<uint>();

    public ushort[] CardIdsInOrder { get; init; } = Array.Empty<ushort>();

    public TriadNpc[] Npcs { get; init; } = Array.Empty<TriadNpc>();

    public string[] NpcNames { get; init; } = Array.Empty<string>();

    public uint[] NpcUnlockQuests { get; init; } = Array.Empty<uint>();

    public string[] NpcUnlockQuestNames { get; init; } = Array.Empty<string>();

    public ushort[] NpcDeckCards { get; init; } = Array.Empty<ushort>();

    public ushort[] NpcRewardCards { get; init; } = Array.Empty<ushort>();

    public int[] CardDropStart { get; init; } = new int[1];

    public ushort[] CardDropNpcs { get; init; } = Array.Empty<ushort>();

    public string[] RuleNames { get; init; } = new string[TriadRuleIds.Count];

    public Dictionary<uint, ushort> NpcIndexByTriadRowId { get; init; } = new();

    public Dictionary<uint, ushort> CardIdByItemId { get; init; } = new();

    public int CardCount => CardIdsInOrder.Length;

    public int NpcCount => Npcs.Length;

    public bool IsCard(int cardId) => cardId > 0 && cardId < Cards.Length && Cards[cardId].IsValid;

    public ReadOnlySpan<uint> UnlockQuestsOf(int npcIndex)
    {
        return NpcUnlockQuests.AsSpan(npcIndex * TriadNpc.UnlockQuestSlots, TriadNpc.UnlockQuestSlots);
    }

    public ReadOnlySpan<string> UnlockQuestNamesOf(int npcIndex)
    {
        return NpcUnlockQuestNames.AsSpan(npcIndex * TriadNpc.UnlockQuestSlots, TriadNpc.UnlockQuestSlots);
    }

    public ReadOnlySpan<ushort> RewardsOf(int npcIndex)
    {
        var npc = Npcs[npcIndex];
        return NpcRewardCards.AsSpan(npc.RewardStart, npc.RewardCount);
    }

    public ReadOnlySpan<ushort> FixedCardsOf(int npcIndex)
    {
        var npc = Npcs[npcIndex];
        return NpcDeckCards.AsSpan(npc.CardStart, npc.FixedCardCount);
    }

    public ReadOnlySpan<ushort> VariableCardsOf(int npcIndex)
    {
        var npc = Npcs[npcIndex];
        return NpcDeckCards.AsSpan(npc.CardStart + TriadNpc.DeckSlots, npc.VariableCardCount);
    }

    public ReadOnlySpan<ushort> NpcsDropping(int cardId)
    {
        if (cardId <= 0 || cardId + 1 >= CardDropStart.Length)
        {
            return default;
        }
        var start = CardDropStart[cardId];
        return CardDropNpcs.AsSpan(start, CardDropStart[cardId + 1] - start);
    }

    public bool IsNpcReward(int cardId)
    {
        return NpcsDropping(cardId).Length > 0;
    }

    public string CardName(int cardId)
    {
        if (!IsCard(cardId))
        {
            return string.Empty;
        }
        return CardNames[cardId];
    }
}
