using AutoTripleTriadGrind.Core.Triad.Match;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class ResultReader
{
    private const int ExpandedRootChildren = 10;

    private const int FlagsNodeIndex = 9;

    private const int FlagChildren = 3;

    public unsafe static MatchOutcome ReadOutcome(AtkUnitBase* result)
    {
        var rootNode = result->RootNode;
        var outcome = FromFlags(NodeWalk.Child(rootNode, FlagsNodeIndex, ExpandedRootChildren));
        if (outcome != MatchOutcome.Unknown)
        {
            return outcome;
        }
        var childCount = NodeWalk.ChildCount(rootNode);
        for (var i = 0; i < childCount; i++)
        {
            outcome = FromFlags(NodeWalk.Child(rootNode, i, childCount));
            if (outcome != MatchOutcome.Unknown)
            {
                return outcome;
            }
        }
        return MatchOutcome.Unknown;
    }

    public unsafe static bool IsReady(AtkUnitBase* result) => result != null && result->IsReady;

    private unsafe static MatchOutcome FromFlags(AtkResNode* flags)
    {
        if (NodeWalk.ChildCount(flags) != FlagChildren)
        {
            return MatchOutcome.Unknown;
        }
        if (NodeWalk.IsVisible(NodeWalk.Child(flags, 2, FlagChildren)))
        {
            return MatchOutcome.Won;
        }
        if (NodeWalk.IsVisible(NodeWalk.Child(flags, 1, FlagChildren)))
        {
            return MatchOutcome.Lost;
        }
        if (!NodeWalk.IsVisible(NodeWalk.Child(flags, 0, FlagChildren)))
        {
            return MatchOutcome.Unknown;
        }
        return MatchOutcome.Drawn;
    }
}
