using AutoTripleTriadGrind.Core.Triad.Data;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Planning;

internal static class NpcEligibility
{
    public static SkipReason Check(int npcIndex, Configuration configuration)
    {
        var npc = TriadData.Set.Npcs[npcIndex];
        if (configuration.SkippedNpcs.Contains(npc.TriadRowId))
        {
            return SkipReason.SkippedByYou;
        }
        var inBattleHall = npc.TerritoryId == TriadDataLoader.BattleHallTerritoryId;
        if (inBattleHall != (Svc.ClientState.TerritoryType == TriadDataLoader.BattleHallTerritoryId))
        {
            if (!inBattleHall)
            {
                return SkipReason.PlayerInBattleHall;
            }
            return SkipReason.BattleHall;
        }
        if (configuration.MaxMatchFee > 0 && npc.Fee > configuration.MaxMatchFee)
        {
            return SkipReason.FeeTooHigh;
        }
        if (!TriadUnlock.IsUnlocked(npcIndex))
        {
            return SkipReason.Locked;
        }
        return SkipReason.None;
    }
}
