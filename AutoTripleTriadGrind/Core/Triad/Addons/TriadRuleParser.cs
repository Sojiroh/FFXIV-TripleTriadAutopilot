using System;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class TriadRuleParser
{
    public static TriadRule? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        var trimmed = text.Trim();
        var ruleId = Find(trimmed);
        if (ruleId != TriadRuleId.None)
        {
            return TriadRules.Create(ruleId);
        }
        var openIndex = trimmed.IndexOf('(');
        var closeIndex = trimmed.LastIndexOf(')');
        if (openIndex <= 0 || closeIndex <= openIndex)
        {
            return null;
        }
        var outerId = Find(trimmed.Substring(0, openIndex).Trim());
        var innerId = Find(trimmed[(openIndex + 1)..closeIndex].Trim());
        if (outerId != TriadRuleId.Roulette && innerId == TriadRuleId.None)
        {
            return null;
        }
        var roulette = new TriadRuleRoulette();
        if (innerId is not (TriadRuleId.None or TriadRuleId.Roulette))
        {
            roulette.Resolve(TriadRules.Create(innerId));
        }
        return roulette;
    }

    public static TriadRuleId Find(string name)
    {
        string[] ruleNames = TriadData.Set.RuleNames;
        for (var i = 1; i < TriadRuleIds.Count; i++)
        {
            var ruleName = ruleNames[i];
            if (ruleName != null && ruleName.Length > 0 && ruleName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return (TriadRuleId)i;
            }
        }
        return TriadRuleId.None;
    }
}
