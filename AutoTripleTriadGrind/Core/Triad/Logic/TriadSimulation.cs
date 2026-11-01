using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadSimulation
{
    private static readonly int[] neighborTable = BuildNeighborTable();

    private readonly List<int> comboList = new(TriadGameState.BoardCells);

    private readonly List<int> nextComboList = new(TriadGameState.BoardCells);

    public readonly List<TriadRule> Rules = new(4);

    public TriadRuleFeatures Features { get; private set; }

    public TriadSpecialRules SpecialRules { get; private set; }

    public void Initialize(IReadOnlyList<TriadRule> primary, IReadOnlyList<TriadRule>? secondary = null)
    {
        Rules.Clear();
        AddCopies(primary);
        if (secondary != null)
        {
            AddCopies(secondary);
        }
        UpdateSpecialRules();
    }

    public void CopyRulesFrom(TriadSimulation source)
    {
        Rules.Clear();
        for (var i = 0; i < source.Rules.Count; i++)
        {
            Rules.Add(source.Rules[i].Clone());
        }
        UpdateSpecialRules();
    }

    public void UseRules(IReadOnlyList<TriadRule> rules)
    {
        Rules.Clear();
        for (var i = 0; i < rules.Count; i++)
        {
            Rules.Add(rules[i]);
        }
        UpdateSpecialRules();
    }

    public void UpdateSpecialRules()
    {
        SpecialRules = TriadSpecialRules.None;
        Features = TriadRuleFeatures.None;
        for (var i = 0; i < Rules.Count; i++)
        {
            SpecialRules |= Rules[i].SpecialRules;
            Features |= Rules[i].Features;
        }
    }

    public bool HasSpecialRule(TriadSpecialRules rule)
    {
        return (SpecialRules & rule) != 0;
    }

    public bool HasFeature(TriadRuleFeatures feature)
    {
        return (Features & feature) != 0;
    }

    public TriadGameState StartGame(TriadDeck deckBlue, TriadDeck deckRed, TriadGameStatus status)
    {
        for (var i = 0; i < Rules.Count; i++)
        {
            Rules[i].OnMatchInit();
        }
        return new TriadGameState(new TriadDeckInstanceManual(deckBlue), new TriadDeckInstanceManual(deckRed))
        {
            Status = status
        };
    }

    public static ReadOnlySpan<int> Neighbors(int boardPosition)
    {
        return neighborTable.AsSpan(boardPosition * TriadSide.Count, TriadSide.Count);
    }

    public bool PlaceCard(TriadGameState state, int cardIndex, TriadDeckInstance deck, TriadOwner owner, int boardPosition)
    {
        var turnMatches = (owner == TriadOwner.Blue && state.Status == TriadGameStatus.InProgressBlue) || (owner == TriadOwner.Red && state.Status == TriadGameStatus.InProgressRed);
        var card = deck.GetCard(cardIndex);
        if (!turnMatches || (uint)boardPosition >= TriadGameState.BoardCells || !state.Board[boardPosition].IsEmpty || card == TriadCards.None)
        {
            return false;
        }
        state.Board[boardPosition] = new TriadBoardSlot
        {
            CardId = card,
            Owner = owner
        };
        state.NumCardsPlaced++;
        if (owner == TriadOwner.Blue)
        {
            state.DeckBlue.OnCardPlaced(cardIndex);
            state.Status = TriadGameStatus.InProgressRed;
        }
        else
        {
            state.DeckRed.OnCardPlaced(cardIndex);
            state.Status = TriadGameStatus.InProgressBlue;
        }
        var valid = owner == TriadOwner.Red || !HasSpecialRule(TriadSpecialRules.IgnoreOwnedCheck);
        var allowsCombo = false;
        if (HasFeature(TriadRuleFeatures.CardPlaced))
        {
            for (var i = 0; i < Rules.Count; i++)
            {
                Rules[i].OnCardPlaced(state, boardPosition);
                allowsCombo |= Rules[i].AllowsCombo;
            }
        }
        comboList.Clear();
        CheckCaptures(state, boardPosition, comboList, 0);
        var comboCounter = 0;
        var currentCombo = comboList;
        var nextCombo = nextComboList;
        while (allowsCombo && currentCombo.Count > 0)
        {
            nextCombo.Clear();
            comboCounter++;
            for (var j = 0; j < currentCombo.Count; j++)
            {
                CheckCaptures(state, currentCombo[j], nextCombo, comboCounter);
            }
            var swapped = nextCombo;
            nextCombo = currentCombo;
            currentCombo = swapped;
        }
        if (HasFeature(TriadRuleFeatures.PostCapture))
        {
            for (var k = 0; k < Rules.Count; k++)
            {
                Rules[k].OnPostCaptures(state, boardPosition);
            }
        }
        if (state.NumCardsPlaced == TriadGameState.BoardCells)
        {
            OnAllCardsPlaced(state);
        }
        return valid;
    }

    public bool PlaceCard(TriadGameState state, ushort cardId, TriadOwner owner, int boardPosition)
    {
        var deck = ((owner == TriadOwner.Blue) ? state.DeckBlue : state.DeckRed);
        return PlaceCard(state, deck.GetCardIndex(cardId), deck, owner, boardPosition);
    }

    private void CheckCaptures(TriadGameState state, int boardPosition, List<int> combo, int comboCounter)
    {
        ReadOnlySpan<int> neighbors = Neighbors(boardPosition);
        var isFirstCheck = comboCounter == 0;
        if (isFirstCheck && HasFeature(TriadRuleFeatures.CaptureNeighbors))
        {
            for (var i = 0; i < Rules.Count; i++)
            {
                Rules[i].OnCheckCaptureNeighbors(state, boardPosition, neighbors, combo);
            }
        }
        var captureMath = isFirstCheck && HasFeature(TriadRuleFeatures.CaptureMath);
        var captureWeights = isFirstCheck && HasFeature(TriadRuleFeatures.CaptureWeights);
        var board = state.Board;
        var owner = board[boardPosition].Owner;
        for (var j = 0; j < TriadSide.Count; j++)
        {
            var neighbor = neighbors[j];
            if (neighbor < 0 || board[neighbor].IsEmpty || board[neighbor].Owner == owner)
            {
                continue;
            }
            var cardNumber = board[boardPosition].Number(j);
            var neighborNumber = board[neighbor].OppositeNumber(j);
            if (captureWeights)
            {
                for (var k = 0; k < Rules.Count; k++)
                {
                    Rules[k].OnCheckCaptureWeights(state, boardPosition, neighbor, captureMath, ref cardNumber, ref neighborNumber);
                }
            }
            var captured = cardNumber > neighborNumber;
            if (captureMath)
            {
                for (var l = 0; l < Rules.Count; l++)
                {
                    Rules[l].OnCheckCaptureMath(state, boardPosition, neighbor, cardNumber, neighborNumber, ref captured);
                }
            }
            if (captured)
            {
                board[neighbor].Owner = owner;
                if (comboCounter > 0)
                {
                    combo.Add(neighbor);
                }
            }
        }
    }

    private void OnAllCardsPlaced(TriadGameState state)
    {
        var blueCount = ((state.DeckBlue.AvailableCardMask != 0) ? 1 : 0);
        var board = state.Board;
        for (var i = 0; i < board.Length; i++)
        {
            if (board[i].Owner == TriadOwner.Blue)
            {
                blueCount++;
            }
        }
        state.Status = ((blueCount > 5) ? TriadGameStatus.BlueWins : ((blueCount == 5) ? TriadGameStatus.BlueDraw : TriadGameStatus.BlueLost));
        if (!HasFeature(TriadRuleFeatures.AllPlaced))
        {
            return;
        }
        for (var j = 0; j < Rules.Count; j++)
        {
            Rules[j].OnAllCardsPlaced(state);
        }
    }

    private void AddCopies(IReadOnlyList<TriadRule> rules)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            Rules.Add(TriadRules.Create(rules[i].Id));
        }
    }

    private static int[] BuildNeighborTable()
    {
        var table = new int[TriadGameState.BoardCells * TriadSide.Count];
        for (var i = 0; i < TriadGameState.BoardCells; i++)
        {
            var column = i % TriadGameState.BoardSize;
            var row = i / TriadGameState.BoardSize;
            var offset = i * TriadSide.Count;
            table[offset] = ((row > 0) ? (i - TriadGameState.BoardSize) : (-1));
            table[offset + 2] = ((row < TriadGameState.BoardSize - 1) ? (i + TriadGameState.BoardSize) : (-1));
            table[offset + 1] = ((column < TriadGameState.BoardSize - 1) ? (i + 1) : (-1));
            table[offset + 3] = ((column > 0) ? (i - 1) : (-1));
        }
        return table;
    }
}

public readonly struct TriadSolverResult
{
    public static readonly TriadSolverResult Zero = new(0f, 0f, 0L);

    public readonly float Wins;

    public readonly float Draws;

    public readonly long Games;

    public readonly float WinChance;

    public readonly float DrawChance;

    public readonly TriadGameStatus Expected;

    public readonly float Score;

    public TriadSolverResult(float wins, float draws, long games)
    {
        Wins = wins;
        Draws = draws;
        Games = games;
        WinChance = ((games <= 0) ? 0f : (wins / (float)games));
        DrawChance = ((games <= 0) ? 0f : (draws / (float)games));
        if (WinChance < 0.25f && DrawChance < 0.25f)
        {
            Score = WinChance / 10f;
            Expected = TriadGameStatus.BlueLost;
        }
        else if (WinChance < DrawChance)
        {
            Score = DrawChance;
            Expected = TriadGameStatus.BlueDraw;
        }
        else
        {
            Score = WinChance + 10f;
            Expected = TriadGameStatus.BlueWins;
        }
    }

    public bool IsBetterThan(in TriadSolverResult other)
    {
        return Score > other.Score;
    }
}
