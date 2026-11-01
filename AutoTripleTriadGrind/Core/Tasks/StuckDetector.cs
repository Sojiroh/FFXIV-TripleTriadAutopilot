using System;
using System.Collections.Generic;
using System.Numerics;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Triad.Match;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Tasks;

internal static class StuckDetector
{
    internal const float StuckMoveThresholdMeters = 1.5f;

    internal const int IdleStallTimeoutMs = 8000;

    internal const int NavWedgeTimeoutMs = 3000;

    internal const float ProgressEpsilonMeters = 1f;

    internal const int FalseStartMs = 1500;

    internal const float OffMeshProbeMeters = 5f;

    private const float UnderWorldClearanceMeters = 10f;

    internal const int OffMeshConfirmMs = 1000;

    internal const int AirborneFreezeMs = 2000;

    private const float AirborneFreezeMeters = 0.25f;

    internal static bool IsPositionFrozenLegit()
    {
        var condition = Svc.Condition;
        if (!condition[ConditionFlag.Casting] && !condition[ConditionFlag.Casting87] && !condition[ConditionFlag.Mounting] && !condition[ConditionFlag.Mounting71] && !condition[ConditionFlag.BetweenAreas] && !condition[ConditionFlag.BetweenAreas51] && !condition[ConditionFlag.OccupiedInCutSceneEvent] && !condition[ConditionFlag.WatchingCutscene])
        {
            return condition[ConditionFlag.WatchingCutscene78];
        }
        return true;
    }

    internal static bool IsOffMesh()
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        var condition = Svc.Condition;
        if (condition[ConditionFlag.InFlight] || condition[ConditionFlag.Swimming] || condition[ConditionFlag.Diving] || condition[ConditionFlag.Jumping] || condition[ConditionFlag.Jumping61] || condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])
        {
            return false;
        }
        var navmesh = NavmeshIPC.Instance;
        if (!navmesh.IsReady())
        {
            return false;
        }
        var position = localPlayer.Position;
        if (navmesh.NearestPointReachable(position).HasValue)
        {
            return false;
        }
        if (!navmesh.NearestPoint(position).HasValue)
        {
            return true;
        }
        if (navmesh.HighestFloor(position, allowUnlandable: false, OffMeshProbeMeters) is { } floor)
        {
            return floor.Y - position.Y > UnderWorldClearanceMeters;
        }
        return false;
    }

    internal static Func<bool> MoveStallAbort(string label)
    {
        var tracker = new MoveStallTracker();
        return () =>
        {
            var stallKind = tracker.Check();
            if (stallKind == StallKind.None)
            {
                return false;
            }
            RunLog.Info($"{label} stalled ({stallKind}); aborting the move");
            return true;
        };
    }

    internal static Func<bool> IdleStallAbort(int timeoutMs)
    {
        Vector3? anchor = null;
        var idleSinceMs = Environment.TickCount64;
        return () =>
        {
            var localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null)
            {
                return false;
            }
            var tickCount = Environment.TickCount64;
            var position = localPlayer.Position;
            if (!anchor.HasValue || Vector3.Distance(anchor.Value, position) > StuckMoveThresholdMeters || NavmeshIPC.Instance.IsBusy() || IsPositionFrozenLegit())
            {
                anchor = position;
                idleSinceMs = tickCount;
                return false;
            }
            return tickCount - idleSinceMs >= timeoutMs;
        };
    }

    internal static Func<bool> AirborneFreezeAbort(string label)
    {
        Vector3? anchor = null;
        var frozenSinceMs = Environment.TickCount64;
        return () =>
        {
            var tickCount = Environment.TickCount64;
            var localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null || !Svc.Condition[ConditionFlag.InFlight])
            {
                anchor = null;
                frozenSinceMs = tickCount;
                return false;
            }
            var position = localPlayer.Position;
            if (!anchor.HasValue || Vector3.Distance(anchor.Value, position) > AirborneFreezeMeters)
            {
                anchor = position;
                frozenSinceMs = tickCount;
                return false;
            }
            if (tickCount - frozenSinceMs < AirborneFreezeMs)
            {
                return false;
            }
            RunLog.Info($"{label} froze in the air for {AirborneFreezeMs}ms at {position}; aborting the descent");
            return true;
        };
    }
}

internal sealed class TriadProgress
{
    private ushort[] queue = Array.Empty<ushort>();

    private int queueNext;

    public TriadPhase Phase { get; private set; }

    public ushort CurrentNpcIndex { get; private set; } = ushort.MaxValue;

    public int NpcMatchesWon { get; private set; }

    public int NpcMatchesLost { get; private set; }

    public int NpcMatchesDrawn { get; private set; }

    public float OptimizerProgress { get; private set; }

    public IReadOnlyList<ushort> Queue => queue;

    public int QueueNext => queueNext;

    public void SetPhase(TriadPhase phase)
    {
        Phase = phase;
    }

    public void SetQueue(ushort[] planned)
    {
        queue = planned;
        queueNext = 0;
    }

    public void SetQueueNext(int queueIndex)
    {
        queueNext = queueIndex;
    }

    public void BeginNpc(ushort npcIndex)
    {
        CurrentNpcIndex = npcIndex;
        NpcMatchesWon = 0;
        NpcMatchesLost = 0;
        NpcMatchesDrawn = 0;
        OptimizerProgress = 0f;
    }

    public void RecordNpcMatch(MatchOutcome outcome)
    {
        switch (outcome)
        {
            case MatchOutcome.Won:
                NpcMatchesWon++;
                break;
            case MatchOutcome.Lost:
                NpcMatchesLost++;
                break;
            case MatchOutcome.Drawn:
                NpcMatchesDrawn++;
                break;
        }
    }

    public void SetOptimizerProgress(float fraction)
    {
        OptimizerProgress = fraction;
    }

    public void Reset()
    {
        Phase = TriadPhase.Idle;
        CurrentNpcIndex = ushort.MaxValue;
        NpcMatchesWon = 0;
        NpcMatchesLost = 0;
        NpcMatchesDrawn = 0;
        OptimizerProgress = 0f;
        queue = Array.Empty<ushort>();
        queueNext = 0;
    }
}
