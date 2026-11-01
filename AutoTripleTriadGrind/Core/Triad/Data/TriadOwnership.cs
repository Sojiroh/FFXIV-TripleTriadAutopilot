using System;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoTripleTriadGrind.Core.Triad.Data;

internal static class TriadOwnership
{
    private const int RefreshIntervalMs = 1000;

    private const uint FirstTrackedNpcRow = 2293762u;

    private static bool[] owned = Array.Empty<bool>();

    private static long nextRefreshAtMs;

    public static int OwnedCount { get; private set; }

    public static bool IsOwned(int cardId) => cardId > 0 && cardId < owned.Length && owned[cardId];

    public unsafe static void Refresh(bool force = false)
    {
        var tickCount = Environment.TickCount64;
        if (!force && tickCount < nextRefreshAtMs)
        {
            return;
        }
        nextRefreshAtMs = tickCount + RefreshIntervalMs;
        var set = TriadData.Set;
        if (owned.Length != set.Cards.Length)
        {
            owned = new bool[set.Cards.Length];
        }
        var uiState = UIState.Instance();
        var ownedCount = 0;
        var cardIdsInOrder = set.CardIdsInOrder;
        foreach (var cardId in cardIdsInOrder)
        {
            var unlocked = uiState != null && uiState->IsTripleTriadCardUnlocked(cardId);
            owned[cardId] = unlocked;
            if (unlocked)
            {
                ownedCount++;
            }
        }
        OwnedCount = ownedCount;
    }

    public unsafe static bool IsNpcBeaten(uint triadRowId)
    {
        if (triadRowId < FirstTrackedNpcRow)
        {
            return false;
        }
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsTripleTriadNpcBeaten(triadRowId);
    }

    public static int MissingRewardCount(int npcIndex)
    {
        var rewards = TriadData.Set.RewardsOf(npcIndex);
        var missing = 0;
        for (var i = 0; i < rewards.Length; i++)
        {
            if (!IsOwned(rewards[i]))
            {
                missing++;
            }
        }
        return missing;
    }
}
