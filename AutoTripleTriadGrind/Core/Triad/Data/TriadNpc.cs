using System.Numerics;

namespace AutoTripleTriadGrind.Core.Triad.Data;

public readonly struct TriadNpc(uint triadRowId, uint eNpcBaseId, uint territoryId, uint mapId, Vector3 position, Vector2 mapCoordinates, bool unlockNeedsAnyQuest, ushort fee, ushort ruleMask, bool usesRegionalRules, byte expansion, int cardStart, byte fixedCardCount, byte variableCardCount, int rewardStart, byte rewardCount)
{
    public const int DeckSlots = 5;

    public const int UnlockQuestSlots = 3;

    public readonly uint TriadRowId = triadRowId;

    public readonly uint ENpcBaseId = eNpcBaseId;

    public readonly uint TerritoryId = territoryId;

    public readonly uint MapId = mapId;

    public readonly Vector3 Position = position;

    public readonly Vector2 MapCoordinates = mapCoordinates;

    public readonly bool UnlockNeedsAnyQuest = unlockNeedsAnyQuest;

    public readonly ushort Fee = fee;

    public readonly ushort RuleMask = ruleMask;

    public readonly bool UsesRegionalRules = usesRegionalRules;

    public readonly byte Expansion = expansion;

    public readonly int CardStart = cardStart;

    public readonly byte FixedCardCount = fixedCardCount;

    public readonly byte VariableCardCount = variableCardCount;

    public readonly int RewardStart = rewardStart;

    public readonly byte RewardCount = rewardCount;

    public bool HasRule(TriadRuleId rule)
    {
        return TriadRuleIds.Has(RuleMask, rule);
    }
}
