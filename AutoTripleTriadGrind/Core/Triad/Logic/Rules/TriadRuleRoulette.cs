using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public sealed class TriadRuleRoulette : TriadRule
{
    public TriadRule? Resolved { get; private set; }

    public override TriadRuleFeatures Features => Resolved?.Features ?? TriadRuleFeatures.None;

    public override TriadSpecialRules SpecialRules => BaseSpecialRules | (Resolved?.SpecialRules ?? TriadSpecialRules.None);

    public override bool AllowsCombo => Resolved?.AllowsCombo ?? BaseAllowsCombo;

    public override bool DeckOrderMatters => Resolved?.DeckOrderMatters ?? BaseDeckOrderMatters;

    public TriadRuleRoulette()
        : base(TriadRuleId.Roulette, TriadRuleFeatures.None, TriadSpecialRules.RandomizeRule)
    {
    }

    public void Resolve(TriadRule? rule)
    {
        Resolved = rule;
    }

    public override TriadRule Clone()
    {
        var roulette = new TriadRuleRoulette();
        roulette.Resolve(Resolved?.Clone());
        return roulette;
    }

    public override void OnMatchInit()
    {
        Resolved = null;
    }

    public override void OnCardPlaced(TriadGameState state, int boardPosition)
    {
        Resolved?.OnCardPlaced(state, boardPosition);
    }

    public override void OnCheckCaptureNeighbors(TriadGameState state, int boardPosition, ReadOnlySpan<int> neighbors, List<int> captures)
    {
        Resolved?.OnCheckCaptureNeighbors(state, boardPosition, neighbors, captures);
    }

    public override void OnCheckCaptureWeights(TriadGameState state, int boardPosition, int neighborPosition, bool reverseActive, ref int cardNumber, ref int neighborNumber)
    {
        Resolved?.OnCheckCaptureWeights(state, boardPosition, neighborPosition, reverseActive, ref cardNumber, ref neighborNumber);
    }

    public override void OnCheckCaptureMath(TriadGameState state, int boardPosition, int neighborPosition, int cardNumber, int neighborNumber, ref bool captured)
    {
        Resolved?.OnCheckCaptureMath(state, boardPosition, neighborPosition, cardNumber, neighborNumber, ref captured);
    }

    public override void OnPostCaptures(TriadGameState state, int boardPosition)
    {
        Resolved?.OnPostCaptures(state, boardPosition);
    }

    public override void OnAllCardsPlaced(TriadGameState state)
    {
        Resolved?.OnAllCardsPlaced(state);
    }

    public override void OnFilterNextCards(TriadGameState state, ref int allowedCardsMask)
    {
        Resolved?.OnFilterNextCards(state, ref allowedCardsMask);
    }
}
