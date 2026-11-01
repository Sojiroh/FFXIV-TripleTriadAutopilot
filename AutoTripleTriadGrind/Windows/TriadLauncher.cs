using System;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Triad.Data;

namespace AutoTripleTriadGrind.Windows;

internal static class TriadLauncher
{
    public enum Readiness : byte
    {
        DataUnavailable,
        NothingPicked,
        NothingReachable,
        AllDone,
        Ready
    }

    public readonly record struct Assessment(TriadRunMode Mode, Readiness Readiness, int Cards, int Npcs, CollectPlanner.Plan Plan);

    private const long RefreshMs = 500L;

    private static long refreshedAtTick = -RefreshMs;

    private static int selectionVersion = -1;

    private static TriadRunMode cachedMode;

    private static Assessment cached;

    private static CachedText sublabelText;

    public static int SelectionVersion { get; private set; }

    public static void MarkSelectionChanged()
    {
        SelectionVersion++;
    }

    public static void Start(TriadRunMode mode)
    {
        Plugin.Instance.Controller.Start(mode);
    }

    public static Assessment Assess(Configuration configuration)
    {
        var runMode = configuration.RunMode;
        var tickCount = Environment.TickCount64;
        if (tickCount - refreshedAtTick < RefreshMs && selectionVersion == SelectionVersion && cachedMode == runMode)
        {
            return cached;
        }
        TriadData.EnsureLoaded();
        TriadOwnership.Refresh();
        cached = ((runMode == TriadRunMode.Farm) ? AssessFarm(configuration) : AssessCollect(configuration));
        refreshedAtTick = tickCount;
        selectionVersion = SelectionVersion;
        cachedMode = runMode;
        return cached;
    }

    public static string Sublabel(in Assessment assessment)
    {
        var key = ((long)assessment.Mode << 48) | ((long)assessment.Cards << 24) | (uint)assessment.Npcs;
        return sublabelText.Get(key, packed =>
        {
            var npcCount = (int)(packed & 0xFFFFFF);
            var cardCount = (int)((packed >> 24) & 0xFFFFFF);
            return ((int)(packed >> 48) == 1) ? Loc.Plural(L.Triad.NpcsCount, npcCount) : Loc.T(L.Triad.StartSub, Loc.Plural(L.Triad.CardsCount, cardCount), Loc.Plural(L.Triad.NpcsCount, npcCount));
        });
    }

    public static string Reason(in Assessment assessment)
    {
        return assessment.Readiness switch
        {
            Readiness.DataUnavailable => Loc.T(L.Triad.ReasonData),
            Readiness.NothingPicked => Loc.T((assessment.Mode == TriadRunMode.Farm) ? L.Triad.ReasonPickNpc : L.Triad.ReasonPickCard),
            Readiness.NothingReachable => Loc.T(L.Triad.ReasonUnreachable),
            Readiness.AllDone => Loc.T(L.Triad.ReasonAllOwned),
            _ => string.Empty,
        };
    }

    private static Assessment AssessCollect(Configuration configuration)
    {
        if (!TriadData.Loaded)
        {
            return new Assessment(TriadRunMode.Collect, Readiness.DataUnavailable, 0, 0, CollectPlanner.Plan.Empty);
        }
        if (configuration.SelectedCards.Count == 0)
        {
            return new Assessment(TriadRunMode.Collect, Readiness.NothingPicked, 0, 0, CollectPlanner.Plan.Empty);
        }
        var plan = CollectPlanner.Build(configuration);
        if (plan.WantedCards == 0)
        {
            return new Assessment(TriadRunMode.Collect, Readiness.AllDone, 0, 0, plan);
        }
        var cards = plan.WantedCards - plan.Unavailable.Length;
        var readiness = ((plan.Assignments.Length == 0) ? Readiness.NothingReachable : Readiness.Ready);
        return new Assessment(TriadRunMode.Collect, readiness, cards, plan.Assignments.Length, plan);
    }

    private static Assessment AssessFarm(Configuration configuration)
    {
        if (!TriadData.Loaded)
        {
            return new Assessment(TriadRunMode.Farm, Readiness.DataUnavailable, 0, 0, CollectPlanner.Plan.Empty);
        }
        var triadSet = TriadData.Set;
        var reachableCount = 0;
        var farmNpcs = configuration.FarmNpcs;
        for (var i = 0; i < farmNpcs.Count; i++)
        {
            if (triadSet.NpcIndexByTriadRowId.TryGetValue(farmNpcs[i], out var npcIndex) && NpcEligibility.Check(npcIndex, configuration) == SkipReason.None)
            {
                reachableCount++;
            }
        }
        var readiness = ((farmNpcs.Count == 0) ? Readiness.NothingPicked : ((reachableCount == 0) ? Readiness.NothingReachable : Readiness.Ready));
        return new Assessment(TriadRunMode.Farm, readiness, 0, reachableCount, CollectPlanner.Plan.Empty);
    }
}
