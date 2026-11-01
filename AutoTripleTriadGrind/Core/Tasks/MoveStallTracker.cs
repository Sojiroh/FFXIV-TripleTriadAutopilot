using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Ipc;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Tasks;

internal sealed class MoveStallTracker
{
    private Vector3? anchor;

    private Vector3? target;

    private float bestTargetDistance = float.MaxValue;

    private int lastWaypointCount;

    private bool followedOnce;

    private bool pathInterrupted;

    private long navWedgeSinceMs = Environment.TickCount64;

    private long idleSinceMs = Environment.TickCount64;

    public StallKind Check()
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return StallKind.None;
        }
        var tickCount = Environment.TickCount64;
        var position = localPlayer.Position;
        var navmesh = NavmeshIPC.Instance;
        var following = navmesh.IsRunning();
        var navBusy = following || navmesh.IsPathfinding();
        var frozenLegit = StuckDetector.IsPositionFrozenLegit();
        if (frozenLegit | navBusy)
        {
            idleSinceMs = tickCount;
        }
        else if (tickCount - idleSinceMs >= StuckDetector.IdleStallTimeoutMs)
        {
            return StallKind.Idle;
        }
        if (frozenLegit)
        {
            navWedgeSinceMs = tickCount;
            anchor = position;
            return StallKind.None;
        }
        if (!following)
        {
            if (followedOnce)
            {
                pathInterrupted = true;
            }
            if (!followedOnce || !navBusy)
            {
                navWedgeSinceMs = tickCount;
            }
            return StallKind.None;
        }
        followedOnce = true;
        var waypointCount = navmesh.NumWaypoints();
        if ((waypointCount == -1) ? DisplacedFromAnchor(position) : ClosedOnWaypoint(navmesh, waypointCount, position))
        {
            navWedgeSinceMs = tickCount;
            return StallKind.None;
        }
        if (tickCount - navWedgeSinceMs < StuckDetector.NavWedgeTimeoutMs)
        {
            return StallKind.None;
        }
        return StallKind.NavWedge;
    }

    private bool DisplacedFromAnchor(Vector3 position)
    {
        var lastAnchor = anchor;
        if (lastAnchor.HasValue && Vector3.Distance(anchor!.Value, position) <= StuckDetector.StuckMoveThresholdMeters)
        {
            return false;
        }
        anchor = position;
        return true;
    }

    private bool ClosedOnWaypoint(NavmeshIPC navmesh, int waypointCount, Vector3 position)
    {
        var waypointReached = target is { } && !pathInterrupted && waypointCount < lastWaypointCount;
        var retarget = target is null || pathInterrupted || waypointCount > lastWaypointCount;
        if (waypointReached | retarget)
        {
            pathInterrupted = false;
            lastWaypointCount = waypointCount;
            target = navmesh.CurrentWaypoint();
            bestTargetDistance = (target is { } waypoint) ? Vector3.Distance(position, waypoint) : float.MaxValue;
            if (target is null)
            {
                return DisplacedFromAnchor(position);
            }
            return waypointReached;
        }
        if (target is null)
        {
            return DisplacedFromAnchor(position);
        }
        var targetDistance = Vector3.Distance(position, target.Value);
        if (targetDistance >= bestTargetDistance - StuckDetector.ProgressEpsilonMeters)
        {
            return false;
        }
        bestTargetDistance = targetDistance;
        return true;
    }
}
