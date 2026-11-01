using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Rules;

public sealed class TriadRuleDraft : TriadRule
{
    public TriadRuleDraft()
        : base(TriadRuleId.Draft, TriadRuleFeatures.None, TriadSpecialRules.IgnoreOwnedCheck)
    {
    }
}

public sealed class TriadRuleRandom : TriadRule
{
    public TriadRuleRandom()
        : base(TriadRuleId.Random, TriadRuleFeatures.None, TriadSpecialRules.RandomizeBlueDeck)
    {
    }
}
