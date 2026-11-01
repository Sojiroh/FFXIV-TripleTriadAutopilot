using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public static class TriadRules
{
    public static TriadRule Create(TriadRuleId id)
    {
        return id switch
        {
            TriadRuleId.Roulette => (TriadRule)new TriadRuleRoulette(),
            TriadRuleId.AllOpen => new TriadRuleAllOpen(),
            TriadRuleId.ThreeOpen => new TriadRuleThreeOpen(),
            TriadRuleId.Same => new TriadRuleSame(),
            TriadRuleId.SuddenDeath => new TriadRuleSuddenDeath(),
            TriadRuleId.Plus => new TriadRulePlus(),
            TriadRuleId.Random => new TriadRuleRandom(),
            TriadRuleId.Order => new TriadRuleOrder(),
            TriadRuleId.Chaos => new TriadRuleChaos(),
            TriadRuleId.Reverse => new TriadRuleReverse(),
            TriadRuleId.FallenAce => new TriadRuleFallenAce(),
            TriadRuleId.Ascension => new TriadRuleAscension(),
            TriadRuleId.Descension => new TriadRuleDescension(),
            TriadRuleId.Swap => new TriadRuleSwap(),
            TriadRuleId.Draft => new TriadRuleDraft(),
            _ => new TriadRuleNone(),
        };
    }

    public static List<TriadRule> FromMask(ushort ruleMask)
    {
        List<TriadRule> rules = new(4);
        for (var i = 1; i < TriadRuleIds.Count; i++)
        {
            if (TriadRuleIds.Has(ruleMask, (TriadRuleId)i))
            {
                rules.Add(Create((TriadRuleId)i));
            }
        }
        return rules;
    }
}

[Flags]
public enum TriadRuleFeatures : byte
{
    None = 0,
    CardPlaced = 1,
    CaptureNeighbors = 2,
    CaptureWeights = 4,
    CaptureMath = 8,
    PostCapture = 0x10,
    AllPlaced = 0x20,
    FilterNext = 0x40
}

public abstract class TriadRule
{
    public TriadRuleId Id { get; }

    protected TriadRuleFeatures BaseFeatures { get; }

    protected TriadSpecialRules BaseSpecialRules { get; }

    protected bool BaseAllowsCombo { get; }

    protected bool BaseDeckOrderMatters { get; }

    public virtual TriadRuleFeatures Features => BaseFeatures;

    public virtual TriadSpecialRules SpecialRules => BaseSpecialRules;

    public virtual bool AllowsCombo => BaseAllowsCombo;

    public virtual bool DeckOrderMatters => BaseDeckOrderMatters;

    protected TriadRule(TriadRuleId id, TriadRuleFeatures features = TriadRuleFeatures.None, TriadSpecialRules specialRules = TriadSpecialRules.None, bool allowsCombo = false, bool deckOrderMatters = false)
    {
        Id = id;
        BaseFeatures = features;
        BaseSpecialRules = specialRules;
        BaseAllowsCombo = allowsCombo;
        BaseDeckOrderMatters = deckOrderMatters;
    }

    public virtual TriadRule Clone()
    {
        return TriadRules.Create(Id);
    }

    public virtual void OnMatchInit()
    {
    }

    public virtual void OnCardPlaced(TriadGameState state, int boardPosition)
    {
    }

    public virtual void OnCheckCaptureNeighbors(TriadGameState state, int boardPosition, ReadOnlySpan<int> neighbors, List<int> captures)
    {
    }

    public virtual void OnCheckCaptureWeights(TriadGameState state, int boardPosition, int neighborPosition, bool reverseActive, ref int cardNumber, ref int neighborNumber)
    {
    }

    public virtual void OnCheckCaptureMath(TriadGameState state, int boardPosition, int neighborPosition, int cardNumber, int neighborNumber, ref bool captured)
    {
    }

    public virtual void OnPostCaptures(TriadGameState state, int boardPosition)
    {
    }

    public virtual void OnScreenUpdate(TriadGameState state)
    {
    }

    public virtual void OnAllCardsPlaced(TriadGameState state)
    {
    }

    public virtual void OnFilterNextCards(TriadGameState state, ref int allowedCardsMask)
    {
    }

    public virtual void OnScoreCard(ushort cardId, ref float score)
    {
    }
}
