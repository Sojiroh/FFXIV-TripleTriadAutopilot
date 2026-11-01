using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Decks;

internal static class TriadDeckBuilder
{
    private static CancellationTokenSource? running;

    private static Task<TriadOptimizedDeck>? build;

    private static TriadDeckOptimizer? optimizer;

    private static uint buildingFor;

    private static ushort buildingRules;

    public static bool IsBuilding
    {
        get
        {
            var buildTask = build;
            if (buildTask != null)
            {
                return !buildTask.IsCompleted;
            }
            return false;
        }
    }

    public static float Progress => optimizer?.Progress ?? 0f;

    public static TriadDeck NpcDeck(int npcIndex)
    {
        var set = TriadData.Set;
        return new TriadDeck(set.FixedCardsOf(npcIndex), set.VariableCardsOf(npcIndex));
    }

    public static ushort RuleMask(int npcIndex, ReadOnlySpan<TriadRuleId> regional)
    {
        var mask = TriadData.Set.Npcs[npcIndex].RuleMask;
        for (var i = 0; i < regional.Length; i++)
        {
            if (regional[i] != TriadRuleId.None)
            {
                mask = TriadRuleIds.With(mask, regional[i]);
            }
        }
        return mask;
    }

    public static bool TryGetCached(Configuration configuration, int npcIndex, ushort ruleMask, out CachedDeck deck)
    {
        var triadRowId = TriadData.Set.Npcs[npcIndex].TriadRowId;
        if (configuration.OptimizedDecks.TryGetValue(triadRowId, out deck!) && deck.RuleMask == ruleMask && deck.OwnedCount == TriadOwnership.OwnedCount && deck.Cards.Length == 5)
        {
            return true;
        }
        deck = null!;
        return false;
    }

    public static void Start(Configuration configuration, int npcIndex, ushort ruleMask)
    {
        var triadRowId = TriadData.Set.Npcs[npcIndex].TriadRowId;
        if (DeckBuildReuse.Serves(build, buildingFor, buildingRules, triadRowId, ruleMask))
        {
            return;
        }
        Cancel();
        var ownedCards = OwnedCards();
        optimizer = new TriadDeckOptimizer(NpcDeck(npcIndex), TriadRules.FromMask(ruleMask), ownedCards);
        running = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, configuration.OptimizerTimeoutSeconds)));
        buildingFor = triadRowId;
        buildingRules = ruleMask;
        var threads = ((configuration.OptimizerThreads > 0) ? configuration.OptimizerThreads : AttgConstants.BackgroundWorkerThreads);
        var job = optimizer;
        var token = running.Token;
        build = Task.Run(() => job.Run(threads, token), CancellationToken.None);
        RunLog.Info($"Building a deck for {TriadData.Set.NpcNames[npcIndex]} from {ownedCards.Length} cards ({job.PossibleDecks} candidates).");
    }

    public static CachedDeck? TryCollect(Configuration configuration)
    {
        var buildTask = build;
        if (buildTask == null || !buildTask.IsCompleted)
        {
            return null;
        }
        build = null;
        if (buildTask.IsFaulted || buildTask.IsCanceled)
        {
            RunLog.Warning("The deck build failed: " + buildTask.Exception?.GetBaseException().Message);
            return null;
        }
        var result = buildTask.Result;
        var cachedDeck = new CachedDeck
        {
            Cards = result.Cards,
            WinChance = result.WinChance,
            OwnedCount = TriadOwnership.OwnedCount,
            RuleMask = buildingRules
        };
        configuration.OptimizedDecks[buildingFor] = cachedDeck;
        configuration.SaveDebounced();
        RunLog.Info($"Deck built: [{string.Join(", ", result.Cards)}], {result.WinChance:P0} estimated, {result.TestedDecks}/{result.PossibleDecks} decks tried.");
        return cachedDeck;
    }

    public static void Cancel()
    {
        running?.Cancel();
        running?.Dispose();
        running = null;
        build = null;
        optimizer = null;
    }

    private static ushort[] OwnedCards()
    {
        var cardIdsInOrder = TriadData.Set.CardIdsInOrder;
        List<ushort> ownedCards = new(TriadOwnership.OwnedCount);
        for (var i = 0; i < cardIdsInOrder.Length; i++)
        {
            if (TriadOwnership.IsOwned(cardIdsInOrder[i]))
            {
                ownedCards.Add(cardIdsInOrder[i]);
            }
        }
        return ownedCards.ToArray();
    }
}
