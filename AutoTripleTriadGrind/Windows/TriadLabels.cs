using System;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Travel;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Windows;

internal static class TriadLabels
{
    private static TriadDataSet? builtFor;

    private static LanguageInfo? builtLanguage;

    private static string[] npcLocations = Array.Empty<string>();

    private static string[] npcSearchKeys = Array.Empty<string>();

    private static string[] cardSearchKeys = Array.Empty<string>();

    private static string[] cardStats = Array.Empty<string>();

    public static string NpcLocation(int npcIndex)
    {
        EnsureBuilt();
        return npcLocations[npcIndex];
    }

    public static string NpcSearchKey(int npcIndex)
    {
        EnsureBuilt();
        return npcSearchKeys[npcIndex];
    }

    public static string CardSearchKey(int cardId)
    {
        EnsureBuilt();
        return cardSearchKeys[cardId];
    }

    public static string CardStats(int cardId)
    {
        EnsureBuilt();
        return cardStats[cardId];
    }

    public static string Expansion(byte expansion)
    {
        return expansion switch
        {
            0 => Loc.T(L.Triad.ExpansionArr),
            1 => Loc.T(L.Triad.ExpansionHw),
            2 => Loc.T(L.Triad.ExpansionSb),
            3 => Loc.T(L.Triad.ExpansionShb),
            4 => Loc.T(L.Triad.ExpansionEw),
            _ => Loc.T(L.Triad.ExpansionDt),
        };
    }

    public static string Skip(SkipReason reason)
    {
        return reason switch
        {
            SkipReason.Locked => Loc.T(L.Triad.SkipLocked),
            SkipReason.BattleHall => Loc.T(L.Triad.SkipBattleHall),
            SkipReason.PlayerInBattleHall => Loc.T(L.Triad.SkipLeaveBattleHall),
            SkipReason.FeeTooHigh => Loc.T(L.Triad.SkipFee),
            SkipReason.NotEnoughMgp => Loc.T(L.Triad.SkipMgp),
            SkipReason.InteractFailed => Loc.T(L.Triad.SkipInteract),
            SkipReason.LossStreak => Loc.T(L.Triad.SkipLossStreak),
            SkipReason.MatchLimit => Loc.T(L.Triad.SkipMatchLimit),
            SkipReason.InventoryFull => Loc.T(L.Triad.SkipInventory),
            SkipReason.SkippedByYou => Loc.T(L.Triad.SkipByYou),
            SkipReason.QuestOffered => Loc.T(L.Triad.SkipQuestOffered),
            _ => Loc.T(L.Triad.SkipUnreachable),
        };
    }

    private static void EnsureBuilt()
    {
        var triadSet = TriadData.Set;
        if (builtFor == triadSet && builtLanguage == Loc.Current)
        {
            return;
        }
        npcLocations = new string[triadSet.NpcCount];
        npcSearchKeys = new string[triadSet.NpcCount];
        for (var npcIndex = 0; npcIndex < triadSet.NpcCount; npcIndex++)
        {
            var triadNpc = triadSet.Npcs[npcIndex];
            var territoryName = TerritoryNames.Of(triadNpc.TerritoryId);
            npcLocations[npcIndex] = Loc.T(L.Triad.NpcLocation, territoryName, triadNpc.MapCoordinates.X.ToString("0.0", Loc.Culture), triadNpc.MapCoordinates.Y.ToString("0.0", Loc.Culture));
            npcSearchKeys[npcIndex] = triadSet.NpcNames[npcIndex] + " " + territoryName;
        }
        cardSearchKeys = new string[triadSet.Cards.Length];
        cardStats = new string[triadSet.Cards.Length];
        for (var cardId = 0; cardId < triadSet.Cards.Length; cardId++)
        {
            cardSearchKeys[cardId] = triadSet.CardNames[cardId] ?? string.Empty;
            if (!triadSet.IsCard(cardId))
            {
                cardStats[cardId] = string.Empty;
                continue;
            }
            var triadCard = triadSet.Cards[cardId];
            cardStats[cardId] = Loc.T(L.Triad.CardStats, triadCard.Stars, Side(triadCard.Up), Side(triadCard.Right), Side(triadCard.Down), Side(triadCard.Left));
        }
        builtFor = triadSet;
        builtLanguage = Loc.Current;
    }

    private static string Side(byte value)
    {
        if (value < 10)
        {
            return value.ToString(Loc.Culture);
        }
        return "A";
    }
}
