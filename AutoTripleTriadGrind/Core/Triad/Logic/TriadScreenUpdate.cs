using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

[Flags]
public enum TriadScreenUpdate : byte
{
    None = 0,
    Rules = 1,
    Board = 2,
    RedDeck = 4,
    BlueDeck = 8,
    SwapWarning = 0x10,
    SwapHints = 0x20
}

public sealed class TriadScreenMemory
{
    private const int MaxBlueHistory = 10;

    private readonly List<ushort[]> blueDeckHistory = new();

    private readonly TriadDeckInstanceScreen deckBlue = new();

    private readonly TriadDeckInstanceScreen deckRed = new();

    private bool hasOpenRule;

    private bool hasRestartRule;

    private bool hasSwapRule;

    private bool swapStartChecked;

    private TriadDeck? lastNpcDeck;

    private ushort[]? playerDeckPattern;

    public TriadSolver Solver { get; private set; } = TriadSolver.CreateLive();

    public TriadGameState GameState { get; }

    public int SwappedBlueCardIndex { get; private set; } = -1;

    public TriadDeckInstanceScreen DeckBlue => deckBlue;

    public TriadDeckInstanceScreen DeckRed => deckRed;

    public TriadScreenMemory()
    {
        GameState = new TriadGameState(deckBlue, deckRed);
    }

    public void UpdatePlayerDeck(TriadDeck playerDeck)
    {
        playerDeckPattern = playerDeck.KnownCards.ToArray();
    }

    public void Reset()
    {
        lastNpcDeck = null;
        blueDeckHistory.Clear();
        swapStartChecked = false;
        SwappedBlueCardIndex = -1;
        playerDeckPattern = null;
        Array.Fill(GameState.Board, TriadBoardSlot.Empty);
        Solver = TriadSolver.CreateLive();
    }

    public TriadScreenUpdate OnNewScan(TriadScreenState screen, TriadDeck npcDeck)
    {
        var update = TriadScreenUpdate.None;
        var continuesPrevious = deckRed.Deck == npcDeck && lastNpcDeck == npcDeck;
        if (continuesPrevious)
        {
            for (var i = 0; i < TriadGameState.BoardCells; i++)
            {
                if (!GameState.Board[i].IsEmpty && screen.Board[i] == TriadCards.None)
                {
                    continuesPrevious = false;
                }
            }
        }
        var simulation = Solver.Simulation;
        if (!SameRules(simulation.Rules, screen.Rules))
        {
            hasSwapRule = false;
            hasRestartRule = false;
            hasOpenRule = false;
            simulation.UseRules(screen.Rules);
            for (var j = 0; j < screen.Rules.Count; j++)
            {
                switch (screen.Rules[j].Id)
                {
                    case TriadRuleId.Swap:
                        hasSwapRule = true;
                        break;
                    case TriadRuleId.SuddenDeath:
                        hasRestartRule = true;
                        break;
                    case TriadRuleId.AllOpen:
                        hasOpenRule = true;
                        break;
                }
            }
            update |= TriadScreenUpdate.Rules;
            continuesPrevious = false;
            deckRed.SetSwappedCard(TriadCards.None, -1);
            Solver.Agent.OnSimulationStart();
            blueDeckHistory.Clear();
            swapStartChecked = false;
        }
        var npcDeckChanged = lastNpcDeck != npcDeck;
        if (npcDeckChanged)
        {
            blueDeckHistory.Clear();
            swapStartChecked = false;
        }
        if (npcDeckChanged || !HandMatches(deckRed, screen.RedHand) || deckRed.Deck != npcDeck)
        {
            update |= TriadScreenUpdate.RedDeck;
            deckRed.Deck = npcDeck;
            lastNpcDeck = npcDeck;
            if (npcDeckChanged)
            {
                Solver.Agent.OnSimulationStart();
            }
            UpdateAvailableRedCards(screen.RedHand, screen.BlueHand, screen.Board, continuesPrevious);
        }
        if (!HandMatches(deckBlue, screen.BlueHand))
        {
            update |= TriadScreenUpdate.BlueDeck;
            deckBlue.UpdateAvailableCards(screen.BlueHand);
        }
        GameState.Status = TriadGameStatus.InProgressBlue;
        GameState.DeckBlue = deckBlue;
        GameState.DeckRed = deckRed;
        GameState.NumCardsPlaced = 0;
        GameState.ForcedCardIndex = ((screen.ForcedBlueCard != TriadCards.None) ? deckBlue.GetCardIndex(screen.ForcedBlueCard) : (-1));
        if (GameState.ForcedCardIndex >= 0 && (deckBlue.AvailableCardMask & (1 << GameState.ForcedCardIndex)) == 0)
        {
            GameState.ForcedCardIndex = -1;
        }
        if (SyncBoard(screen))
        {
            update |= TriadScreenUpdate.Board;
            for (var k = 0; k < simulation.Rules.Count; k++)
            {
                simulation.Rules[k].OnScreenUpdate(GameState);
            }
        }
        if (hasSwapRule && !swapStartChecked && GameState.NumCardsPlaced <= 1)
        {
            swapStartChecked = true;
            update |= DetectSwapOnGameStart();
        }
        return update;
    }

    private bool SyncBoard(TriadScreenState screen)
    {
        var changed = false;
        var board = GameState.Board;
        for (var i = 0; i < TriadGameState.BoardCells; i++)
        {
            var screenCard = screen.Board[i];
            var wasEmpty = board[i].IsEmpty;
            var isNowEmpty = screenCard == TriadCards.None;
            if (wasEmpty && !isNowEmpty)
            {
                changed = true;
                board[i] = new TriadBoardSlot
                {
                    CardId = screenCard,
                    Owner = screen.BoardOwners[i]
                };
            }
            else if (!wasEmpty & isNowEmpty)
            {
                changed = true;
                board[i] = TriadBoardSlot.Empty;
            }
            else if (!wasEmpty && (board[i].Owner != screen.BoardOwners[i] || board[i].CardId != screenCard))
            {
                changed = true;
                board[i] = new TriadBoardSlot
                {
                    CardId = screenCard,
                    Owner = screen.BoardOwners[i]
                };
            }
            if (!board[i].IsEmpty)
            {
                GameState.NumCardsPlaced++;
            }
        }
        return changed;
    }

    private static bool SameRules(List<TriadRule> current, List<TriadRule> screen)
    {
        if (current.Count != screen.Count)
        {
            return false;
        }
        for (var i = 0; i < current.Count; i++)
        {
            var found = false;
            for (var j = 0; j < screen.Count; j++)
            {
                if (screen[j].Id == current[i].Id && ResolvedId(screen[j]) == ResolvedId(current[i]))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }

    private static TriadRuleId ResolvedId(TriadRule rule)
    {
        if (rule is not TriadRuleRoulette roulette)
        {
            return TriadRuleId.None;
        }
        return roulette.Resolved?.Id ?? TriadRuleId.None;
    }

    private static bool HandMatches(TriadDeckInstanceScreen deck, ushort[] hand)
    {
        if (deck.Cards.Length < hand.Length)
        {
            return false;
        }
        for (var i = 0; i < hand.Length; i++)
        {
            if (hand[i] != deck.Cards[i])
            {
                return false;
            }
        }
        return true;
    }

    private void UpdateAvailableRedCards(ushort[] screenRed, ushort[] screenBlue, ushort[] screenBoard, bool continuesPrevious)
    {
        var deck = deckRed.Deck!;
        var handSize = deckRed.Cards.Length;
        deckRed.NumPlaced = 0;
        if (!continuesPrevious)
        {
            deckRed.NumUnknownPlaced = 0;
        }
        var knownCount = deck.KnownCards.Count;
        var unknownInHand = handSize - knownCount;
        var poolBitOffset = handSize + knownCount;
        if (deck.UnknownPool.Count > 0)
        {
            deckRed.UnknownPoolMask = (1 << deck.UnknownPool.Count) - 1 << poolBitOffset;
            for (var i = 0; i < screenRed.Length; i++)
            {
                var screenCard = screenRed[i];
                if (screenCard != TriadCards.None && screenCard != TriadCards.Hidden && deck.UnknownPool.Contains(screenCard))
                {
                    deckRed.UnknownPoolMask |= 1 << i;
                }
            }
        }
        var availableCardMask = (1 << deck.CardCount) - 1 << handSize;
        var board = GameState.Board;
        var trackPlacements = true;
        var isGameStart = false;
        if (!continuesPrevious)
        {
            var boardCardCount = 0;
            for (var j = 0; j < screenBoard.Length; j++)
            {
                if (screenBoard[j] != TriadCards.None)
                {
                    boardCardCount++;
                }
            }
            if (boardCardCount <= 1)
            {
                isGameStart = true;
                deckRed.Cards = TriadDeckInstanceScreen.NewHand();
                deckRed.AvailableCardMask = availableCardMask;
                deckRed.NumPlaced = 0;
                deckRed.NumUnknownPlaced = 0;
            }
            else
            {
                trackPlacements = false;
            }
        }
        if (!trackPlacements)
        {
            deckRed.UpdateAvailableCards(screenRed);
            deckRed.AvailableCardMask = availableCardMask;
            return;
        }
        List<int> placedIndices = new(5);
        List<ushort> bluePlayed = new(5);
        var visibleKnown = 0;
        var visibleUnknown = 0;
        var hiddenCount = 0;
        var visibleCount = 0;
        for (var k = 0; k < handSize; k++)
        {
            var screenCard = screenRed[k];
            if (screenCard == TriadCards.None)
            {
                var previousCard = deckRed.Cards[k];
                if (previousCard != TriadCards.None && previousCard != TriadCards.Hidden)
                {
                    placedIndices.Add(k);
                }
                deckRed.AvailableCardMask &= ~(1 << k);
                deckRed.NumPlaced++;
                continue;
            }
            if (screenCard == TriadCards.Hidden)
            {
                hiddenCount++;
                continue;
            }
            var fromUnknownPool = (deckRed.UnknownPoolMask & (1 << k)) != 0;
            visibleUnknown += (fromUnknownPool ? 1 : 0);
            visibleKnown += ((!fromUnknownPool) ? 1 : 0);
            visibleCount++;
            deckRed.AvailableCardMask |= 1 << k;
            var knownIndex = deck.KnownCards.IndexOf(screenCard);
            var poolIndex = deck.UnknownPool.IndexOf(screenCard);
            if (knownIndex >= 0)
            {
                deckRed.AvailableCardMask &= ~(1 << knownIndex + handSize);
            }
            else if (poolIndex >= 0)
            {
                deckRed.AvailableCardMask &= ~(1 << poolIndex + handSize + knownCount);
            }
        }
        var previousBlueHand = (isGameStart ? TriadDeckInstanceScreen.NewHand() : deckBlue.Cards);
        for (var l = 0; l < previousBlueHand.Length; l++)
        {
            if (previousBlueHand[l] != TriadCards.None && screenBlue[l] == TriadCards.None)
            {
                bluePlayed.Add(previousBlueHand[l]);
            }
        }
        for (var m = 0; m < screenBoard.Length; m++)
        {
            var boardCard = screenBoard[m];
            var isOpenCell = isGameStart || board[m].IsEmpty;
            if (isOpenCell && boardCard != TriadCards.None)
            {
                var cardIndex = deckRed.GetCardIndex(boardCard);
                if (!bluePlayed.Contains(boardCard) && cardIndex >= 0)
                {
                    placedIndices.Add(cardIndex);
                }
            }
        }
        screenRed.AsSpan(0, handSize).CopyTo(deckRed.Cards);
        for (var n = 0; n < placedIndices.Count; n++)
        {
            var slotBit = 1 << placedIndices[n];
            deckRed.AvailableCardMask &= ~slotBit;
            if ((deckRed.UnknownPoolMask & slotBit) != 0)
            {
                deckRed.NumUnknownPlaced++;
            }
        }
        if (hiddenCount == 0 && visibleCount + deckRed.NumPlaced == handSize)
        {
            deckRed.AvailableCardMask &= (1 << handSize) - 1;
        }
        else if (deckRed.NumUnknownPlaced + visibleUnknown >= unknownInHand || (visibleKnown >= handSize - unknownInHand && hiddenCount == 0))
        {
            deckRed.AvailableCardMask &= (1 << handSize + knownCount) - 1;
        }
    }

    private TriadScreenUpdate DetectSwapOnGameStart()
    {
        deckRed.SetSwappedCard(TriadCards.None, -1);
        for (var i = 0; i < deckBlue.Cards.Length; i++)
        {
            if (deckBlue.Cards[i] == TriadCards.None)
            {
                return TriadScreenUpdate.None;
            }
        }
        if (hasRestartRule && IsSuddenDeathRestart())
        {
            return TriadScreenUpdate.None;
        }
        if (playerDeckPattern == null)
        {
            playerDeckPattern = (ushort[])deckBlue.Cards.Clone();
        }
        if (blueDeckHistory.Count > MaxBlueHistory)
        {
            blueDeckHistory.RemoveAt(0);
        }
        blueDeckHistory.Add((ushort[])deckBlue.Cards.Clone());
        var found = FindSwappedCardVisible(out var swappedIndex, out var otherIndex, out var swappedCard);
        if (!found && playerDeckPattern != null)
        {
            found = FindSwappedCard(deckBlue.Cards, playerDeckPattern, out swappedIndex, out otherIndex, out swappedCard);
            if (!found)
            {
                var commonCards = FindCommonCards();
                if (commonCards != null)
                {
                    found = FindSwappedCard(deckBlue.Cards, commonCards, out swappedIndex, out otherIndex, out swappedCard);
                }
            }
        }
        if (!found)
        {
            SwappedBlueCardIndex = -1;
            return TriadScreenUpdate.SwapWarning;
        }
        deckRed.SetSwappedCard(swappedCard, otherIndex);
        SwappedBlueCardIndex = swappedIndex;
        return TriadScreenUpdate.SwapHints;
    }

    private bool FindSwappedCard(ushort[] screenCards, ushort[] expectedCards, out int swappedIndex, out int otherIndex, out ushort swappedCard)
    {
        swappedIndex = -1;
        otherIndex = -1;
        swappedCard = TriadCards.None;
        var mismatchCount = 0;
        var knownRedCount = 0;
        var compareCount = Math.Min(screenCards.Length, expectedCards.Length);
        for (var i = 0; i < compareCount; i++)
        {
            var screenCard = screenCards[i];
            if (screenCard != expectedCards[i] && screenCard != TriadCards.None)
            {
                mismatchCount++;
                swappedIndex = i;
                otherIndex = deckRed.GetCardIndex(screenCard);
                swappedCard = expectedCards[i];
                if (otherIndex >= 0)
                {
                    knownRedCount++;
                }
            }
        }
        return mismatchCount == 1 && knownRedCount == 1;
    }

    private bool FindSwappedCardVisible(out int swappedIndex, out int otherIndex, out ushort swappedCard)
    {
        swappedIndex = -1;
        otherIndex = -1;
        swappedCard = TriadCards.None;
        var candidateCount = 0;
        var dealtRedCount = 0;
        var deck = deckRed.Deck!;
        var blueCards = deckBlue.Cards;
        for (var i = 0; i < deckRed.Cards.Length; i++)
        {
            var redCard = deckRed.Cards[i];
            if (redCard != TriadCards.None && redCard != TriadCards.Hidden && deck.GetCardIndex(redCard) < 0)
            {
                otherIndex = i;
                swappedCard = redCard;
                for (var j = 0; j < blueCards.Length; j++)
                {
                    if (deck.GetCardIndex(blueCards[j]) >= 0)
                    {
                        swappedIndex = j;
                        candidateCount++;
                    }
                }
            }
            dealtRedCount += ((redCard != TriadCards.None) ? 1 : 0);
        }
        if (dealtRedCount < blueCards.Length)
        {
            var board = GameState.Board;
            for (var k = 0; k < board.Length; k++)
            {
                if (board[k].IsEmpty || board[k].Owner != TriadOwner.Red || deck.GetCardIndex(board[k].CardId) >= 0)
                {
                    continue;
                }
                swappedCard = board[k].CardId;
                otherIndex = 100;
                for (var l = 0; l < blueCards.Length; l++)
                {
                    if (deck.GetCardIndex(blueCards[l]) >= 0)
                    {
                        swappedIndex = l;
                        candidateCount++;
                    }
                }
            }
        }
        return candidateCount == 1;
    }

    private ushort[]? FindCommonCards()
    {
        if (blueDeckHistory.Count <= 1)
        {
            return null;
        }
        var commonCards = new ushort[blueDeckHistory[0].Length];
        Dictionary<ushort, int> counts = new();
        for (var i = 0; i < commonCards.Length; i++)
        {
            counts.Clear();
            var bestCard = TriadCards.None;
            var bestCount = 0;
            for (var j = 0; j < blueDeckHistory.Count; j++)
            {
                var card = blueDeckHistory[j][i];
                if (card != TriadCards.None)
                {
                    var count = counts.GetValueOrDefault(card) + 1;
                    counts[card] = count;
                    if (count > bestCount)
                    {
                        bestCount = count;
                        bestCard = card;
                    }
                }
            }
            if (bestCount < 2)
            {
                return null;
            }
            commonCards[i] = bestCard;
        }
        return commonCards;
    }

    private bool IsSuddenDeathRestart()
    {
        var unknownCount = 0;
        var dealtCount = 0;
        var deck = deckRed.Deck!;
        for (var i = 0; i < deckRed.Cards.Length; i++)
        {
            var card = deckRed.Cards[i];
            if (deck.GetCardIndex(card) < 0)
            {
                unknownCount++;
            }
            if (card != TriadCards.None && card != TriadCards.Hidden)
            {
                dealtCount++;
            }
        }
        if (dealtCount < 4 || unknownCount <= 1)
        {
            if (dealtCount >= 4)
            {
                return !hasOpenRule;
            }
            return false;
        }
        return true;
    }
}
