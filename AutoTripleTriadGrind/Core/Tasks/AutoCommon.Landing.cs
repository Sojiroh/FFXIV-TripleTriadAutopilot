using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;
using Dalamud.Game.ClientState.Conditions;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int MaxLandingSpots = 3;

    private const float LandingRingRadiusMeters = 6f;

    private const int LandingRingPoints = 6;

    private const int LandingCandidateCount = 7;

    private const float LandingFloorHalfExtentMeters = 3f;

    private const float LandingProbeLiftMeters = 30f;

    private const float LandingArriveMeters = 1.5f;

    private const int LandingFlightWatchdogMs = 20000;

    private const int LandingDismountWatchdogMs = 6000;

    private const int GroundDismountWatchdogMs = 8000;

    private const float LandingBackOffMeters = 8f;

    private const int LandingBackOffMs = 1500;

    private const float LandsOnTheSpot = 0f;

    private readonly Vector3[] landingSpots = new Vector3[LandingCandidateCount];

    protected async Task<bool> SafeDismount(string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }
        var around = ECommons.DalamudServices.Svc.Objects.LocalPlayer?.Position ?? Vector3.Zero;
        return await LandAndDismount(around, LandsOnTheSpot, scope);
    }

    protected async Task<bool> LandAndDismount(Vector3 around, float standOffMeters, string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }
        Status = "Landing";
        var from = ECommons.DalamudServices.Svc.Objects.LocalPlayer?.Position ?? around;
        var spotCount = FindLandingSpots(around, standOffMeters, from, allowUnlandable: false, landingSpots);
        if (spotCount == 0)
        {
            spotCount = FindLandingSpots(around, standOffMeters, from, allowUnlandable: true, landingSpots);
        }
        if (spotCount == 0)
        {
            Diag(scope + ": no floor to land on around " + FormatPosition(around) + "; descending where the flight ended");
            return await DescendAndDismount(scope);
        }
        var attempts = Math.Min(spotCount, MaxLandingSpots);
        for (var spotIndex = 0; spotIndex < attempts; spotIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            var spot = landingSpots[spotIndex];
            var legScope = $"{scope}-land#{spotIndex + 1}";
            Diag($"{legScope}: flying {DistanceTo(spot):F0}m to a landable spot at {FormatPosition(spot)}");
            var flight = new MoveOp(move => move.MoveInZone(spot, walkMovement.WithTolerance(LandingArriveMeters), null));
            await RunCancellable(flight, LandingFlightWatchdogMs, legScope, StuckDetector.MoveStallAbort(legScope));
            var fault = flight.Fault;
            if (fault != null)
            {
                Diag(legScope + ": the landing flight faulted: " + fault.Message);
            }
            if (await DescendAndDismount(legScope))
            {
                return true;
            }
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            await BackOffInFlight(legScope);
        }
        Warn($"{scope}: could not land near {FormatPosition(around)} after {attempts} spot(s) ({ConditionTag()})");
        return false;
    }

    private async Task<bool> DescendAndDismount(string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }
        await DismountViaOp(scope, LandingDismountWatchdogMs, StuckDetector.AirborneFreezeAbort(scope));
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        Diag(scope + ": the descent did not land (" + ConditionTag() + ")");
        CancelDescent();
        return false;
    }

    private async Task<bool> GroundDismount(string scope)
    {
        await DismountViaOp(scope, GroundDismountWatchdogMs);
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        Diag(scope + ": still mounted after the dismount (" + ConditionTag() + ")");
        return false;
    }

    private void CancelDescent()
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            UseGeneralAction(JumpGeneralActionId);
        }
    }

    private async Task BackOffInFlight(string scope)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null || !ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return;
        }
        var hoverSpot = localPlayer.Position with { Y = localPlayer.Position.Y + LandingBackOffMeters };
        Diag($"{scope}: backing off {LandingBackOffMeters:F0}m upward");
        NavmeshIPC.Instance.MoveAlong([hoverSpot], fly: true);
        await DelayMs(LandingBackOffMs);
        NavmeshIPC.Instance.Stop();
    }

    private static int FindLandingSpots(Vector3 around, float standOffMeters, Vector3 from, bool allowUnlandable, Vector3[] spots)
    {
        var navmesh = NavmeshIPC.Instance;
        var ringAroundTarget = standOffMeters > LandsOnTheSpot;
        var count = 0;
        if (!ringAroundTarget && LandingFloor(navmesh, around, allowUnlandable) is { } centerSpot)
        {
            spots[count++] = centerSpot;
        }
        var ringRadius = (ringAroundTarget ? standOffMeters : LandingRingRadiusMeters);
        for (var i = 0; i < LandingRingPoints; i++)
        {
            var angle = (float)Math.PI * 2f * i / LandingRingPoints;
            var point = around + new Vector3(MathF.Cos(angle) * ringRadius, 0f, MathF.Sin(angle) * ringRadius);
            if (LandingFloor(navmesh, point, allowUnlandable) is { } ringSpot)
            {
                spots[count++] = ringSpot;
            }
        }
        if (!ringAroundTarget)
        {
            return count;
        }
        SortNearestFirst(spots, count, from);
        if (LandingFloor(navmesh, around, allowUnlandable) is { } fallbackSpot)
        {
            spots[count++] = fallbackSpot;
        }
        return count;
    }

    private static void SortNearestFirst(Vector3[] spots, int count, Vector3 from)
    {
        for (var i = 1; i < count; i++)
        {
            var spot = spots[i];
            var spotDistance = GroundDistance.SquaredBetween(from, spot);
            var insertAt = i;
            while (insertAt > 0 && GroundDistance.SquaredBetween(from, spots[insertAt - 1]) > spotDistance)
            {
                spots[insertAt] = spots[insertAt - 1];
                insertAt--;
            }
            spots[insertAt] = spot;
        }
    }

    private static Vector3? LandingFloor(NavmeshIPC navmesh, Vector3 point, bool allowUnlandable) => navmesh.PointOnFloor(point with { Y = point.Y + LandingProbeLiftMeters }, allowUnlandable, LandingFloorHalfExtentMeters);
}
