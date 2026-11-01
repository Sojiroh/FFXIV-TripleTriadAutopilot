using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;
using Dalamud.Game.ClientState.Conditions;
using clib.TaskSystem;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private enum GroundRouteKind : byte
    {
        Unknown,
        Complete,
        Partial
    }

    private enum LegMode : byte
    {
        OnFoot,
        GroundMount,
        Flight
    }

    private readonly record struct GroundRoute(GroundRouteKind Kind, Vector3 End, float LengthMeters, float ShortfallMeters);

    private readonly record struct LegPlan(LegMode Mode, Vector3 Target, float ShortfallMeters, bool Reachable)
    {
        public bool Rides => Mode != LegMode.OnFoot;

        public float ToleranceFor(float wantedMeters)
        {
            if (!(ShortfallMeters <= 0f))
            {
                return MathF.Max(MinLegToleranceMeters, wantedMeters - ShortfallMeters);
            }
            return wantedMeters;
        }
    }

    private const int RouteQueryTimeoutMs = 5000;

    private const float PartialRouteGapMeters = 0.75f;

    private const float RouteGoalHalfExtentMeters = 5f;

    private const float FlightMinMeters = 80f;

    private const float MinLegToleranceMeters = 1.5f;

    private static readonly MovementConfig flightMovement = MovementConfig.Default.WithOptions(MovementOptions.Mount | MovementOptions.Fly);

    private static readonly MovementConfig groundMountMovement = MovementConfig.Default.WithOptions(MovementOptions.Mount);

    private static readonly MovementConfig walkMovement = MovementConfig.Default;

    private async Task<LegPlan> PlanLeg(uint territoryId, Vector3 destination, float reachMeters, float mountMinMeters, bool mountingAllowed, bool groundStalled, string scope)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return new LegPlan(LegMode.OnFoot, destination, 0f, Reachable: true);
        }
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return new LegPlan(LegMode.Flight, destination, 0f, Reachable: true);
        }
        var mountable = mountingAllowed && FreeToMount();
        var mounted = ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted];
        var flightAvailable = mountable && FlightAccess.IsAvailableIn(territoryId);
        var straight = Vector3.Distance(localPlayer.Position, destination);
        if (flightAvailable && (groundStalled || straight > FlightMinMeters))
        {
            return new LegPlan(LegMode.Flight, destination, 0f, Reachable: true);
        }
        var route = await ProbeGroundRoute(localPlayer.Position, destination);
        var plan = PlanFromRoute(in route, destination, straight, reachMeters, mountMinMeters, mountable, mounted, flightAvailable);
        Diag(scope + ": " + DescribeLegPlan(in plan, in route, straight, flightAvailable));
        return plan;
    }

    private static LegPlan PlanFromRoute(in GroundRoute route, Vector3 destination, float straight, float reachMeters, float mountMinMeters, bool mountable, bool mounted, bool flightAvailable)
    {
        if (route.Kind == GroundRouteKind.Unknown)
        {
            return new LegPlan(LegModeFor(mountable && (mounted || straight > mountMinMeters), flightAvailable), destination, 0f, Reachable: true);
        }
        var reachable = route.Kind == GroundRouteKind.Complete || route.ShortfallMeters <= reachMeters;
        if (!reachable)
        {
            if (!flightAvailable)
            {
                return new LegPlan(LegMode.OnFoot, route.End, route.ShortfallMeters, Reachable: false);
            }
            return new LegPlan(LegMode.Flight, destination, 0f, Reachable: true);
        }
        var legMode = LegModeFor(mountable && (mounted || route.LengthMeters > mountMinMeters), flightAvailable && route.LengthMeters > FlightMinMeters);
        if (route.Kind != GroundRouteKind.Complete && legMode != LegMode.Flight)
        {
            return new LegPlan(legMode, route.End, route.ShortfallMeters, Reachable: true);
        }
        return new LegPlan(legMode, destination, 0f, Reachable: true);
    }

    private static LegMode LegModeFor(bool ride, bool fly)
    {
        if (!ride)
        {
            return LegMode.OnFoot;
        }
        if (!fly)
        {
            return LegMode.GroundMount;
        }
        return LegMode.Flight;
    }

    private static string DescribeLegPlan(in LegPlan plan, in GroundRoute route, float straight, bool flightAvailable)
    {
        var routeSummary = route.Kind switch
        {
            GroundRouteKind.Complete => $"the ground route is {route.LengthMeters:F0}m for {straight:F0}m straight",
            GroundRouteKind.Partial => $"the ground route ends {route.ShortfallMeters:F0}m short of the spot after {route.LengthMeters:F0}m",
            _ => $"no ground route answer for {straight:F0}m straight",
        };
        var reachSummary = (plan.Reachable ? $"going {plan.Mode}" : "out of reach on the ground");
        return $"{routeSummary}, flight {(flightAvailable ? "available" : "unavailable")}; {reachSummary}";
    }

    private async Task<GroundRoute> ProbeGroundRoute(Vector3 from, Vector3 to)
    {
        var navmesh = NavmeshIPC.Instance;
        if (!navmesh.NearestPoint(from).HasValue || navmesh.NearestPoint(to) is not { } goal)
        {
            return default;
        }
        Task<List<Vector3>>? pending = NavmeshPathfindIPC.Instance.Pathfind(from, goal, fly: false);
        if (pending == null)
        {
            return default;
        }
        var deadline = Environment.TickCount64 + RouteQueryTimeoutMs;
        while (!pending.IsCompleted)
        {
            if (CancelToken.IsCancellationRequested || Environment.TickCount64 >= deadline)
            {
                return default;
            }
            await NextFrame();
        }
        if (!pending.IsCompletedSuccessfully)
        {
            Diag("Route: the ground route query faulted: " + pending.Exception?.GetBaseException().Message);
            return default;
        }
        return MeasureGroundRoute(pending.Result, from, goal);
    }

    private static GroundRoute MeasureGroundRoute(List<Vector3>? waypoints, Vector3 from, Vector3 goal)
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            return default;
        }
        var endIndex = waypoints.Count - 2;
        var lengthMeters = Vector3.Distance(from, waypoints[0]);
        for (var i = 1; i <= endIndex; i++)
        {
            lengthMeters += Vector3.Distance(waypoints[i - 1], waypoints[i]);
        }
        var endPoint = waypoints[endIndex];
        var shortfallMeters = Vector3.Distance(endPoint, goal);
        if (!(shortfallMeters > PartialRouteGapMeters))
        {
            return new GroundRoute(GroundRouteKind.Complete, goal, lengthMeters, 0f);
        }
        return new GroundRoute(GroundRouteKind.Partial, endPoint, lengthMeters, shortfallMeters);
    }

    private static MovementConfig MovementFor(LegMode mode, float tolerance)
    {
        var movement = mode switch
        {
            LegMode.Flight => flightMovement,
            LegMode.GroundMount => groundMountMovement,
            _ => walkMovement,
        };
        return movement.WithTolerance(tolerance);
    }
}
