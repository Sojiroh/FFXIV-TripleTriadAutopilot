using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public sealed class TriadRuleSame : TriadRule
{
    public TriadRuleSame()
        : base(TriadRuleId.Same, TriadRuleFeatures.CardPlaced | TriadRuleFeatures.CaptureNeighbors, TriadSpecialRules.None, allowsCombo: true)
    {
    }

    public override void OnCheckCaptureNeighbors(TriadGameState state, int boardPosition, ReadOnlySpan<int> neighbors, List<int> captures)
    {
        var board = state.Board;
        ref var placed = ref board[boardPosition];
        var matchCount = 0;
        var captureMask = 0;
        for (var i = 0; i < TriadSide.Count; i++)
        {
            var neighbor = neighbors[i];
            if (neighbor >= 0 && !board[neighbor].IsEmpty && placed.Number(i) == board[neighbor].OppositeNumber(i))
            {
                matchCount++;
                if (board[neighbor].Owner != placed.Owner)
                {
                    captureMask |= 1 << i;
                }
            }
        }
        if (matchCount < 2)
        {
            return;
        }
        for (var j = 0; j < TriadSide.Count; j++)
        {
            if ((captureMask & (1 << j)) != 0)
            {
                board[neighbors[j]].Owner = placed.Owner;
                captures.Add(neighbors[j]);
            }
        }
    }
}

public sealed class TriadRulePlus : TriadRule
{
    public TriadRulePlus()
        : base(TriadRuleId.Plus, TriadRuleFeatures.CardPlaced | TriadRuleFeatures.CaptureNeighbors, TriadSpecialRules.None, allowsCombo: true)
    {
    }

    public override void OnCheckCaptureNeighbors(TriadGameState state, int boardPosition, ReadOnlySpan<int> neighbors, List<int> captures)
    {
        var board = state.Board;
        var owner = board[boardPosition].Owner;
        for (var i = 0; i < TriadSide.Count; i++)
        {
            var neighbor = neighbors[i];
            if (neighbor < 0 || board[neighbor].IsEmpty || board[neighbor].Owner == owner)
            {
                continue;
            }
            var sum = board[boardPosition].Number(i) + board[neighbor].OppositeNumber(i);
            var matched = false;
            for (var j = 0; j < TriadSide.Count; j++)
            {
                var otherNeighbor = neighbors[j];
                if (otherNeighbor >= 0 && j != i && !board[otherNeighbor].IsEmpty && board[boardPosition].Number(j) + board[otherNeighbor].OppositeNumber(j) == sum)
                {
                    matched = true;
                    if (board[otherNeighbor].Owner != owner)
                    {
                        board[otherNeighbor].Owner = owner;
                        captures.Add(otherNeighbor);
                    }
                }
            }
            if (matched)
            {
                board[neighbor].Owner = owner;
                captures.Add(neighbor);
            }
        }
    }
}

public sealed class TriadRuleReverse : TriadRule
{
    private const float MaxSideSum = 40f;

    public TriadRuleReverse()
        : base(TriadRuleId.Reverse, TriadRuleFeatures.CaptureMath)
    {
    }

    public override void OnCheckCaptureMath(TriadGameState state, int boardPosition, int neighborPosition, int cardNumber, int neighborNumber, ref bool captured)
    {
        captured = cardNumber < neighborNumber;
    }

    public override void OnScoreCard(ushort cardId, ref float score)
    {
        score = 1f - (float)TriadCards.Get(cardId).SideSum / MaxSideSum;
    }
}

public sealed class TriadRuleFallenAce : TriadRule
{
    public TriadRuleFallenAce()
        : base(TriadRuleId.FallenAce, TriadRuleFeatures.CaptureWeights)
    {
    }

    public override void OnCheckCaptureWeights(TriadGameState state, int boardPosition, int neighborPosition, bool reverseActive, ref int cardNumber, ref int neighborNumber)
    {
        if (reverseActive)
        {
            if (cardNumber == TriadCards.MaxNumber && neighborNumber == TriadCards.MinNumber)
            {
                cardNumber = 0;
            }
            return;
        }
        if (cardNumber == TriadCards.MinNumber && neighborNumber == TriadCards.MaxNumber)
        {
            neighborNumber = 0;
        }
    }
}

public sealed class TriadRuleNone : TriadRule
{
    public TriadRuleNone()
        : base(TriadRuleId.None)
    {
    }
}

public sealed class TriadRuleThreeOpen : TriadRule
{
    public TriadRuleThreeOpen()
        : base(TriadRuleId.ThreeOpen, TriadRuleFeatures.None, TriadSpecialRules.SelectVisible3)
    {
    }
}

public sealed class TriadRuleChaos : TriadRule
{
    public TriadRuleChaos()
        : base(TriadRuleId.Chaos, TriadRuleFeatures.None, TriadSpecialRules.BlueCardSelection)
    {
    }
}
