using System;
using System.Collections.Generic;
using System.Numerics;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Triad.Data;
using ECommons.GameHelpers;

namespace AutoTripleTriadGrind.Core.Planning;

internal static class CollectPlanner
{
    public readonly record struct Plan(NpcAssignment[] Assignments, UnavailableCard[] Unavailable, int WantedCards)
    {
        public static readonly Plan Empty = new(Array.Empty<NpcAssignment>(), Array.Empty<UnavailableCard>(), 0);
    }

    public static Plan Build(Configuration configuration, IReadOnlySet<int>? excludedNpcs = null)
    {
        var set = TriadData.Set;
        var wanted = WantedCards(configuration);
        var eligibility = new SkipReason[set.NpcCount];
        for (var i = 0; i < eligibility.Length; i++)
        {
            eligibility[i] = ((excludedNpcs != null && excludedNpcs.Contains(i)) ? SkipReason.Unreachable : NpcEligibility.Check(i, configuration));
        }
        var coverage = new int[set.NpcCount];
        for (var j = 0; j < wanted.Count; j++)
        {
            var droppers = set.NpcsDropping(wanted[j]);
            for (var k = 0; k < droppers.Length; k++)
            {
                coverage[droppers[k]]++;
            }
        }
        var cardsByNpc = new Dictionary<ushort, List<ushort>>();
        var unavailable = new List<UnavailableCard>();
        for (var l = 0; l < wanted.Count; l++)
        {
            var cardId = wanted[l];
            var npcIndex = PickNpc(set.NpcsDropping(cardId), eligibility, coverage, out var reason);
            if (npcIndex == ushort.MaxValue)
            {
                unavailable.Add(new UnavailableCard(cardId, reason));
                continue;
            }
            if (!cardsByNpc.TryGetValue(npcIndex, out var npcCards))
            {
                npcCards = new List<ushort>();
                cardsByNpc[npcIndex] = npcCards;
            }
            npcCards.Add(cardId);
        }
        return new Plan(Order(cardsByNpc), unavailable.ToArray(), wanted.Count);
    }

    public static List<ushort> WantedCards(Configuration configuration)
    {
        var set = TriadData.Set;
        var wanted = new List<ushort>(configuration.SelectedCards.Count);
        var cardIdsInOrder = set.CardIdsInOrder;
        foreach (var cardId in cardIdsInOrder)
        {
            if (configuration.SelectedCards.Contains(cardId) && set.IsNpcReward(cardId) && !CardRegistrar.IsCollected(cardId))
            {
                wanted.Add(cardId);
            }
        }
        return wanted;
    }

    private static ushort PickNpc(ReadOnlySpan<ushort> droppers, SkipReason[] eligibility, int[] coverage, out SkipReason reason)
    {
        var best = ushort.MaxValue;
        reason = SkipReason.Unreachable;
        for (var i = 0; i < droppers.Length; i++)
        {
            var candidate = droppers[i];
            if (eligibility[candidate] != SkipReason.None)
            {
                reason = MoreTelling(reason, eligibility[candidate]);
            }
            else if (best == ushort.MaxValue || coverage[candidate] > coverage[best])
            {
                best = candidate;
            }
        }
        return best;
    }

    private static SkipReason MoreTelling(SkipReason current, SkipReason candidate)
    {
        if (candidate != SkipReason.Locked && current != SkipReason.Unreachable)
        {
            return current;
        }
        return candidate;
    }

    private static NpcAssignment[] Order(Dictionary<ushort, List<ushort>> cardsByNpc)
    {
        var set = TriadData.Set;
        var remaining = new List<ushort>(cardsByNpc.Keys);
        var assignments = new NpcAssignment[remaining.Count];
        var territory = (Player.Available ? Player.Territory.RowId : 0u);
        var position = (Player.Available ? Player.Position : Vector3.Zero);
        for (var i = 0; i < assignments.Length; i++)
        {
            var index = NearestNext(remaining, territory, position);
            var npcIndex = remaining[index];
            remaining.RemoveAt(index);
            assignments[i] = new NpcAssignment(npcIndex, cardsByNpc[npcIndex].ToArray());
            territory = set.Npcs[npcIndex].TerritoryId;
            position = set.Npcs[npcIndex].Position;
        }
        return assignments;
    }

    private static int NearestNext(List<ushort> remaining, uint territory, Vector3 position)
    {
        var npcs = TriadData.Set.Npcs;
        var best = 0;
        var foundInTerritory = false;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < remaining.Count; i++)
        {
            var npc = npcs[remaining[i]];
            TriadNpc current;
            if (npc.TerritoryId == territory)
            {
                var distance = Vector3.DistanceSquared(npc.Position, position);
                if (!foundInTerritory || distance < bestDistance)
                {
                    best = i;
                    foundInTerritory = true;
                    bestDistance = distance;
                }
            }
            else if (!foundInTerritory)
            {
                current = npcs[remaining[best]];
                if (npc.Expansion < current.Expansion || (npc.Expansion == current.Expansion && npc.TerritoryId < current.TerritoryId))
                {
                    best = i;
                }
            }
        }
        return best;
    }
}
