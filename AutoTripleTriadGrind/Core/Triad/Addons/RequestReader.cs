using System;
using AutoTripleTriadGrind.Core.Triad.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class RequestReader
{
    private const int MinRootChildren = 9;

    private const int RegionalRulesIndex = 7;

    private const int RuleEntryChildren = 3;

    private const int RuleTextIndex = 2;

    private const int RegionalSlots = 2;

    public unsafe static bool TryReadRegionalRules(AtkUnitBase* request, Span<TriadRuleId> regional)
    {
        regional.Clear();
        var rootNode = request->RootNode;
        var rootChildCount = NodeWalk.ChildCount(rootNode);
        if (rootChildCount < MinRootChildren)
        {
            return false;
        }
        var regionalNode = NodeWalk.Child(rootNode, RegionalRulesIndex, rootChildCount);
        var entryCount = NodeWalk.ChildCount(regionalNode);
        if (entryCount == 0)
        {
            return false;
        }
        var regionalCount = 0;
        for (var i = 0; i < Math.Min(entryCount, RegionalSlots); i++)
        {
            string? ruleText = NodeWalk.Text(NodeWalk.ComponentChild(NodeWalk.Child(regionalNode, i, entryCount), RuleTextIndex, RuleEntryChildren));
            if (!string.IsNullOrWhiteSpace(ruleText))
            {
                var ruleId = TriadRuleParser.Find(ruleText.Trim());
                if (ruleId != TriadRuleId.None)
                {
                    regional[regionalCount++] = ruleId;
                }
            }
        }
        return true;
    }
}
