using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class FlightAccess
{
    private static readonly Dictionary<uint, uint> completionFlagSetByTerritory = new();

    public unsafe static bool IsAvailableIn(uint territoryId)
    {
        if (Svc.Condition[ConditionFlag.Mounted] && Svc.ClientState.TerritoryType == territoryId)
        {
            return Control.CanFly;
        }
        var flagSet = CompletionFlagSetOf(territoryId);
        if (flagSet == 0)
        {
            return false;
        }
        var playerState = PlayerState.Instance();
        if (playerState != null)
        {
            return playerState->IsAetherCurrentZoneComplete(flagSet);
        }
        return false;
    }

    private static uint CompletionFlagSetOf(uint territoryId)
    {
        if (completionFlagSetByTerritory.TryGetValue(territoryId, out var cachedFlagSet))
        {
            return cachedFlagSet;
        }
        var flagSet = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryId)?.AetherCurrentCompFlgSet.RowId ?? 0;
        completionFlagSetByTerritory[territoryId] = flagSet;
        return flagSet;
    }
}

internal readonly record struct CityInn(uint CityTerritoryId, uint InnTerritoryId, uint InnkeeperBaseId, Vector3 InnkeeperPosition);
