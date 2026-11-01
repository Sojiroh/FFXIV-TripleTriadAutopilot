using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic.Agents;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadDeckOptimizer
{
    private struct CardPool
    {
        public ushort[][] PriorityLists;

        public ushort[] CommonList;

        public int[] SlotTypes;
    }

    private sealed class ScoredCard(ushort cardId, float score)
    {
        public readonly ushort CardId = cardId;

        public float Score = score;
    }

    private readonly struct Candidate(ushort[] cards, int seed)
    {
        public readonly ushort[] Cards = cards;

        public readonly int Seed = seed;

        public bool IsValid
        {
            get
            {
                for (var i = 0; i < Cards.Length; i++)
                {
                    for (var j = i + 1; j < Cards.Length; j++)
                    {
                        if (Cards[i] == Cards[j])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }

    private sealed class SlotIterator
    {
        private readonly ushort[][] slotLists = new ushort[5][];

        private readonly bool[] slotIsCommon = new bool[5];

        public SlotIterator(CardPool pool)
        {
            for (var i = 0; i < 5; i++)
            {
                var slotType = pool.SlotTypes[i];
                slotIsCommon[i] = slotType == DeckSlotCommon;
                slotLists[i] = ((slotType == DeckSlotCommon) ? pool.CommonList : pool.PriorityLists[slotType]);
            }
        }

        public IEnumerable<Candidate> Decks()
        {
            var lists = slotLists;
            for (var first = 0; first < lists[0].Length; first++)
            {
                for (var second = LoopStart(1, first, -1, -1, -1); second < lists[1].Length; second++)
                {
                    for (var third = LoopStart(2, first, second, -1, -1); third < lists[2].Length; third++)
                    {
                        for (var fourth = LoopStart(3, first, second, third, -1); fourth < lists[3].Length; fourth++)
                        {
                            for (var fifth = LoopStart(4, first, second, third, fourth); fifth < lists[4].Length; fifth++)
                            {
                                var cards = new ushort[5]
                                {
                                    lists[0][first],
                                    lists[1][second],
                                    lists[2][third],
                                    lists[3][fourth],
                                    lists[4][fifth]
                                };
                                yield return new Candidate(cards, Seed(first, second, third, fourth, fifth));
                            }
                        }
                    }
                }
            }
        }

        private int LoopStart(int slot, int first, int second, int third, int fourth)
        {
            if (!slotIsCommon[slot])
            {
                return 0;
            }
            if (slot >= 4 && slotIsCommon[3])
            {
                return fourth + 1;
            }
            if (slot >= 3 && slotIsCommon[2])
            {
                return third + 1;
            }
            if (slot >= 2 && slotIsCommon[1])
            {
                return second + 1;
            }
            if (slot < 1 || !slotIsCommon[0])
            {
                return 0;
            }
            return first + 1;
        }

        private static int Seed(int first, int second, int third, int fourth, int fifth)
        {
            var hash = 13;
            hash = hash * 37 + first;
            hash = hash * 37 + second;
            hash = hash * 37 + third;
            hash = hash * 37 + fourth;
            hash = hash * 37 + fifth;
            return hash;
        }
    }

    private const int GamesPerDeck = 2000;

    private const int PriorityToBuild = 10;

    private const int CommonToBuild = 20;

    private const int CommonPercentDroppedPerPrioritySlot = 10;

    private const int CommonMaxStars = 3;

    private const int FourStarSlots = 2;

    private const int FiveStarSlots = 1;

    private const int DeckSlotCommon = -1;

    private static readonly ushort[] starterDeck = new ushort[5] { 1, 3, 6, 7, 10 };

    private readonly TriadSolver solver = new TriadSolver(new TriadRandomAgent(0));

    private readonly TriadDeck npcDeck;

    private readonly bool deckOrderMatters;

    private CardPool pool;

    private long possibleDecks;

    private long testedDecks;

    private static readonly Comparison<ScoredCard> byScoreDescending = (ScoredCard left, ScoredCard right) => right.Score.CompareTo(left.Score);

    public bool HasPool { get; }

    public long PossibleDecks => possibleDecks;

    public float Progress
    {
        get
        {
            if (possibleDecks <= 0)
            {
                return 0f;
            }
            return Math.Clamp((float)Interlocked.Read(in testedDecks) / (float)possibleDecks, 0f, 1f);
        }
    }

    public TriadDeckOptimizer(TriadDeck npcDeck, IReadOnlyList<TriadRule> rules, ReadOnlySpan<ushort> ownedCards)
    {
        this.npcDeck = npcDeck;
        solver.InitializeSimulation(rules);
        var simulationRules = solver.Simulation.Rules;
        for (var i = 0; i < simulationRules.Count; i++)
        {
            deckOrderMatters |= simulationRules[i].DeckOrderMatters;
        }
        HasPool = BuildCardPool(ownedCards, simulationRules);
        if (HasPool)
        {
            possibleDecks = CountPossibleDecks();
        }
    }

    public static float CardScore(in TriadCard card)
    {
        var maxSide = Math.Max(Math.Max(card.Up, card.Right), Math.Max(card.Down, card.Left));
        return ((float)card.SideSum / 4f / 10f * 1f + (float)(int)maxSide / 10f * 0.75f + (float)(card.Stars - 1) / 4f * 0.2f) / 1.95f;
    }

    public TriadOptimizedDeck Run(int threads, CancellationToken cancelToken)
    {
        if (!HasPool)
        {
            return new TriadOptimizedDeck(starterDeck, 0f, 0L, 0L);
        }
        var sync = new object();
        var bestScore = 0;
        var bestDeck = starterDeck;
        using var workers = new ThreadLocal<TriadSolver>(solver.CreateWorkerCopy);
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, threads),
            CancellationToken = cancelToken
        };
        try
        {
            Parallel.ForEach(new SlotIterator(pool).Decks(), parallelOptions, (Candidate candidate) =>
            {
                if (!candidate.IsValid)
                {
                    return;
                }
                var testDeck = new TriadDeck(candidate.Cards, default);
                var score = ScoreDeck(workers.Value!, testDeck, candidate.Seed, cancelToken);
                Interlocked.Increment(ref testedDecks);
                if (score <= Volatile.Read(in bestScore))
                {
                    return;
                }
                lock (sync)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDeck = candidate.Cards;
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        return new TriadOptimizedDeck(bestDeck, (float)bestScore / (float)(GamesPerDeck * 2), Interlocked.Read(in testedDecks), possibleDecks);
    }

    private int ScoreDeck(TriadSolver worker, TriadDeck testDeck, int seed, CancellationToken cancelToken)
    {
        var randomAgent = new TriadRandomAgent(seed);
        var totalPoints = 0;
        for (var i = 0; i < GamesPerDeck / 2; i++)
        {
            if (cancelToken.IsCancellationRequested)
            {
                break;
            }
            var redStartState = worker.StartSimulation(testDeck, npcDeck, TriadGameStatus.InProgressRed);
            worker.RunSimulation(redStartState, randomAgent, randomAgent);
            totalPoints += Points(redStartState.Status);
            var blueStartState = worker.StartSimulation(testDeck, npcDeck, TriadGameStatus.InProgressBlue);
            worker.RunSimulation(blueStartState, randomAgent, randomAgent);
            totalPoints += Points(blueStartState.Status);
        }
        return totalPoints;
    }

    private static int Points(TriadGameStatus status)
    {
        return status switch
        {
            TriadGameStatus.BlueWins => 2,
            TriadGameStatus.BlueDraw => 1,
            _ => 0,
        };
    }

    private long CountPossibleDecks()
    {
        var combinations = 1L;
        var commonSlots = 0;
        for (var i = 0; i < pool.SlotTypes.Length; i++)
        {
            var slotType = pool.SlotTypes[i];
            if (slotType == DeckSlotCommon)
            {
                commonSlots++;
            }
            else
            {
                combinations *= pool.PriorityLists[slotType].Length;
            }
        }
        var orderings = 1L;
        for (var j = 0; j < commonSlots; j++)
        {
            combinations *= pool.CommonList.Length - j;
            orderings *= j + 1;
        }
        return combinations / orderings;
    }

    private bool BuildCardPool(ReadOnlySpan<ushort> ownedCards, List<TriadRule> rules)
    {
        List<TriadRule> effectiveRules = new(rules.Count);
        var hasReverse = false;
        var ascensionIndex = -1;
        var descensionIndex = -1;
        for (var i = 0; i < rules.Count; i++)
        {
            effectiveRules.Add(rules[i]);
            switch (rules[i].Id)
            {
                case TriadRuleId.Reverse:
                    hasReverse = true;
                    break;
                case TriadRuleId.Ascension:
                    ascensionIndex = i;
                    break;
                case TriadRuleId.Descension:
                    descensionIndex = i;
                    break;
            }
        }
        var ascensionActive = ascensionIndex >= 0;
        if (hasReverse && ascensionIndex >= 0)
        {
            ascensionActive = false;
            effectiveRules.RemoveAt(ascensionIndex);
            effectiveRules.Add(new TriadRuleDescension());
        }
        else if (hasReverse && descensionIndex >= 0)
        {
            ascensionActive = true;
            effectiveRules.RemoveAt(descensionIndex);
            effectiveRules.Add(new TriadRuleAscension());
        }
        List<ScoredCard> allCards = new();
        List<ScoredCard> upToFourStarCards = new();
        List<ScoredCard> upToThreeStarCards = new();
        for (var j = 0; j < ownedCards.Length; j++)
        {
            var cardId = ownedCards[j];
            if (!TriadCards.IsPlayable(cardId))
            {
                continue;
            }
            ref readonly var card = ref TriadCards.Get(cardId);
            var scoredCard = new ScoredCard(cardId, CardScore(in card));
            for (var k = 0; k < effectiveRules.Count; k++)
            {
                effectiveRules[k].OnScoreCard(cardId, ref scoredCard.Score);
            }
            if (!hasReverse)
            {
                allCards.Add(scoredCard);
                if (card.Stars <= 4)
                {
                    upToFourStarCards.Add(scoredCard);
                }
            }
            if (card.Stars <= CommonMaxStars)
            {
                upToThreeStarCards.Add(scoredCard);
            }
        }
        pool = new CardPool
        {
            SlotTypes = new int[5]
        };
        Array.Fill(pool.SlotTypes, DeckSlotCommon);
        if (upToThreeStarCards.Count == 0)
        {
            return false;
        }
        var candidateLists = new List<ScoredCard>[2] { allCards, upToFourStarCards };
        var slotsPerList = new int[2] { FiveStarSlots, FourStarSlots - FiveStarSlots };
        var slot = (deckOrderMatters ? 1 : 0);
        List<List<ScoredCard>> priorityLists = new(2);
        for (var listIndex = 0; listIndex < candidateLists.Length; listIndex++)
        {
            var cards = candidateLists[listIndex];
            if (slotsPerList[listIndex] <= 0 || cards.Count == 0)
            {
                cards.Clear();
                continue;
            }
            for (var n = 0; n < slotsPerList[listIndex]; n++)
            {
                for (; slot < pool.SlotTypes.Length - 1 && pool.SlotTypes[slot] != DeckSlotCommon; slot++)
                {
                }
                pool.SlotTypes[slot] = priorityLists.Count;
            }
            priorityLists.Add(cards);
        }
        if (ascensionActive)
        {
            ApplyAscensionFilter(upToThreeStarCards, priorityLists);
        }
        pool.PriorityLists = new ushort[priorityLists.Count][];
        for (var listIndex = 0; listIndex < priorityLists.Count; listIndex++)
        {
            pool.PriorityLists[listIndex] = TopCards(priorityLists[listIndex], PriorityToBuild);
        }
        var prioritySlotCount = 0;
        for (var slotIndex = 0; slotIndex < pool.SlotTypes.Length; slotIndex++)
        {
            prioritySlotCount += ((pool.SlotTypes[slotIndex] >= 0) ? 1 : 0);
        }
        var limit = CommonToBuild - CommonToBuild * prioritySlotCount * CommonPercentDroppedPerPrioritySlot / 100;
        pool.CommonList = TopCards(upToThreeStarCards, limit);
        return true;
    }

    private static ushort[] TopCards(List<ScoredCard> cards, int limit)
    {
        cards.Sort(byScoreDescending);
        var count = Math.Min(limit, cards.Count);
        var cardIds = new ushort[count];
        for (var i = 0; i < count; i++)
        {
            cardIds[i] = cards[i].CardId;
        }
        return cardIds;
    }

    private static void ApplyAscensionFilter(List<ScoredCard> common, List<List<ScoredCard>> priority)
    {
        var listCount = priority.Count + 1;
        var bestType = TriadCardType.None;
        var bestAverage = 0f;
        for (var typeId = 1; typeId < 5; typeId++)
        {
            var typeScore = 0f;
            for (var j = 0; j < listCount; j++)
            {
                var cards = ((j == 0) ? common : priority[j - 1]);
                var scoreSum = 0f;
                var matchCount = 0;
                for (var k = 0; k < cards.Count; k++)
                {
                    if ((int)TriadCards.Get(cards[k].CardId).Type == typeId)
                    {
                        scoreSum += cards[k].Score;
                        matchCount++;
                    }
                }
                typeScore += ((matchCount == 0) ? 0f : (scoreSum / (float)matchCount));
            }
            typeScore /= (float)listCount;
            if (bestAverage <= 0f || typeScore > bestAverage)
            {
                bestAverage = typeScore;
                bestType = (TriadCardType)typeId;
            }
        }
        if (bestType == TriadCardType.None)
        {
            return;
        }
        Boost(common, bestType, 1000f);
        for (var l = 0; l < priority.Count; l++)
        {
            Boost(priority[l], bestType, 1000f);
        }
    }

    private static void Boost(List<ScoredCard> cards, TriadCardType type, float amount)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            if (TriadCards.Get(cards[i].CardId).Type == type)
            {
                cards[i].Score += amount;
            }
        }
    }
}
