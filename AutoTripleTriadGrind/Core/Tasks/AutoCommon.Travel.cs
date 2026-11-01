using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;
using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private enum ZoneTravelResult
    {
        Arrived,
        Failed,
        LeftZone
    }

    private enum LegOutcome
    {
        EndedShort,
        Stalled,
        Faulted,
        MountFailed,
        Remount,
        FalseStart
    }

    private enum TeleportPurpose
    {
        Shortcut,
        Recovery,
        OffMesh
    }

    private const int TravelTeleportWatchdogMs = 60000;

    private const int TravelNavmeshWaitMs = 60000;

    private const int TravelNavmeshPollFrames = 60;

    private const int TravelPlayerReadyWaitMs = 30000;

    private const int TravelBaseBudgetMs = 60000;

    private const int TravelMaxBudgetMs = 900000;

    private const float TravelMinSpeedMetersPerSecond = 3f;

    private const int TravelProgressLogMs = 15000;

    private const int MaxZoneEntries = 2;

    private const int MaxTravelStalls = 5;

    private const int StallsBeforeUnstick = 2;

    private const int StallsBeforeTeleportRecovery = 3;

    private const int MaxFalseStarts = 3;

    private const int MaxLandmassTeleports = 2;

    private const int FalseStartSettleMs = 1500;

    private const int StillnessConfirmMs = 300;

    private const int StillnessPollMs = 100;

    private const float StillnessMeters = 0.2f;

    private const float TeleportShortcutMinSavingMeters = 300f;

    private const float TeleportRecoveryMinHopMeters = 50f;

    private const float TeleportMovedMeters = 3f;

    private const float MountMinMeters = 30f;

    private const float SnapLiftMeters = 3f;

    private const float SnapHalfExtentMeters = 5f;

    private const float SnapMaxDropMeters = 10f;

    private const float SnapReportMeters = 1f;

    private const uint JumpGeneralActionId = 2u;

    private const int UnstickSettleMs = 1000;

    private const float UnstickStepHalfExtentMeters = 6f;

    private const float UnstickStepMinMeters = 1.5f;

    private const float UnstickStepToleranceMeters = 0.5f;

    private const int UnstickStepWatchdogMs = 8000;

    private const float UnstickSideStepMeters = 4f;

    private const float UnstickSideStepHalfExtentMeters = 3f;

    private const float AetheryteFloorHalfExtentMeters = 10f;

    private bool outsideMovementWarned;

    protected async Task<bool> TravelTo(uint territoryId, Vector3 destination, float arriveWithin, bool groundStalled = false)
    {
        var zoneName = TerritoryNames.Of(territoryId);
        Diag($"Travel: to {zoneName} ({territoryId}) at {FormatPosition(destination)} within {arriveWithin:F1}m, starting in territory {ECommons.DalamudServices.Svc.ClientState.TerritoryType} ({ConditionTag()})");
        for (var entry = 1; entry <= MaxZoneEntries; entry++)
        {
            if (!await WaitForPlayerReady())
            {
                return false;
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId && !await EnterTerritory(territoryId, destination, zoneName))
            {
                return false;
            }
            var zoneTravelResult = await TravelWithinZone(territoryId, destination, arriveWithin, zoneName, groundStalled);
            if (zoneTravelResult != ZoneTravelResult.LeftZone)
            {
                return zoneTravelResult == ZoneTravelResult.Arrived;
            }
            Diag($"Travel: left {zoneName} on the way (now in territory {ECommons.DalamudServices.Svc.ClientState.TerritoryType}); heading back in");
        }
        Warn("Travel: kept leaving " + zoneName + " on the way; giving up");
        return false;
    }

    private async Task<bool> WaitForPlayerReady()
    {
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }
        if (PlayerReady())
        {
            return true;
        }
        Status = "Waiting for the character to be ready";
        var ready = await WaitUntilTimed(PlayerReady, TravelPlayerReadyWaitMs, "travel-player-ready");
        if (!ready && !CancelToken.IsCancellationRequested)
        {
            Warn("Travel: the character never became ready to move");
        }
        return ready;
    }

    private async Task<bool> EnterTerritory(uint territoryId, Vector3 destination, string zoneName)
    {
        if (EntranceRoutes.TryGet(territoryId, out var route))
        {
            return await EnterThroughEntrance(route);
        }
        Diag($"Travel: in territory {ECommons.DalamudServices.Svc.ClientState.TerritoryType}, teleporting toward {zoneName}");
        var reached = false;
        await RunWithStatusPinned("Teleporting to " + zoneName, async () =>
        {
            reached = await TeleportToTerritory(territoryId, destination, "travel-teleport", TravelTeleportWatchdogMs);
        });
        if (reached)
        {
            return true;
        }
        if (!CancelToken.IsCancellationRequested)
        {
            Warn($"Travel: could not reach {zoneName} (still in territory {ECommons.DalamudServices.Svc.ClientState.TerritoryType})");
        }
        return false;
    }

    private async Task<ZoneTravelResult> TravelWithinZone(uint territoryId, Vector3 destination, float arriveWithin, string zoneName, bool groundStalled)
    {
        await WaitForNavmeshReady(TravelNavmeshWaitMs, TravelNavmeshPollFrames);
        if (CancelToken.IsCancellationRequested)
        {
            return ZoneTravelResult.Failed;
        }
        var target = SnapToFloor(destination);
        if (!Arrived(target, destination, arriveWithin))
        {
            await TryTeleportShortcut(territoryId, target, "travel-shortcut", TeleportPurpose.Shortcut);
            await TryAethernetShortcut(territoryId, target);
        }
        var mountingAllowed = TerritoryAllowsMount(territoryId);
        var budgetMs = TravelBudgetMs(target);
        var deadline = Environment.TickCount64 + budgetMs;
        var stalls = 0;
        var falseStarts = 0;
        var landmassTeleports = 0;
        var teleportRecoveryUsed = false;
        for (var leg = 1; ; leg++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return ZoneTravelResult.Failed;
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId)
            {
                return ZoneTravelResult.LeftZone;
            }
            if (Arrived(target, destination, arriveWithin))
            {
                Diag($"Travel: arrived in {zoneName}, {DistanceTo(destination):F1}m from the spot ({ConditionTag()})");
                return ZoneTravelResult.Arrived;
            }
            var remainingMs = deadline - Environment.TickCount64;
            if (remainingMs <= 0)
            {
                Warn($"Travel: ran out of time ({budgetMs / 1000}s) {DistanceTo(target):F0}m short of the spot in {zoneName}");
                return ZoneTravelResult.Failed;
            }
            var scope = $"travel-leg#{leg}";
            var plan = await PlanLeg(territoryId, target, arriveWithin, MountMinMeters, mountingAllowed, groundStalled || stalls > 0, scope);
            if (CancelToken.IsCancellationRequested)
            {
                return ZoneTravelResult.Failed;
            }
            if (!plan.Reachable)
            {
                if (landmassTeleports < MaxLandmassTeleports)
                {
                    landmassTeleports++;
                    if (await TryTeleportToConnectedAetheryte(territoryId, target, arriveWithin, scope))
                    {
                        continue;
                    }
                }
                Warn($"Travel: no ground route reaches the spot in {zoneName} from here or from an attuned aetheryte (the nearest floor is {plan.ShortfallMeters:F0}m short of it), and flying is not available; giving up");
                return ZoneTravelResult.Failed;
            }
            var legOutcome = await RunTravelLeg(plan, target, arriveWithin, mountingAllowed, (int)remainingMs, scope, zoneName);
            if (legOutcome == LegOutcome.Remount || CancelToken.IsCancellationRequested || Arrived(target, destination, arriveWithin))
            {
                continue;
            }
            if (legOutcome == LegOutcome.MountFailed)
            {
                mountingAllowed = false;
                Diag(scope + ": could not mount here; walking the rest of the way");
            }
            if (legOutcome == LegOutcome.FalseStart && falseStarts < MaxFalseStarts)
            {
                falseStarts++;
                await RecoverFromFalseStart(scope, falseStarts);
                continue;
            }
            NavmeshIPC.Instance.Stop();
            if (await RecoverIfOffMesh(territoryId, target, scope))
            {
                continue;
            }
            stalls++;
            if (stalls >= MaxTravelStalls)
            {
                Warn($"Travel: stuck {stalls} times {DistanceTo(target):F0}m short of the spot in {zoneName}; giving up");
                return ZoneTravelResult.Failed;
            }
            if (stalls >= StallsBeforeTeleportRecovery && !teleportRecoveryUsed)
            {
                teleportRecoveryUsed = true;
                if (await TryTeleportShortcut(territoryId, target, scope + "-recovery", TeleportPurpose.Recovery))
                {
                    continue;
                }
            }
            if (stalls >= StallsBeforeUnstick)
            {
                await Unstick(scope, target, stalls);
            }
            else
            {
                Diag(scope + ": re-pathing from here");
            }
        }
    }

    private async Task<LegOutcome> RunTravelLeg(LegPlan plan, Vector3 target, float arriveWithin, bool mountingAllowed, int watchdogMs, string scope, string zoneName)
    {
        var ride = plan.Rides;
        var canRemount = mountingAllowed && !ride;
        var label = (ride ? ("Riding to the spot in " + zoneName) : ("Walking to the spot in " + zoneName));
        var remount = false;
        var tracker = new MoveStallTracker();
        var nextProgressLogAt = Environment.TickCount64 + TravelProgressLogMs;
        Diag($"{scope}: going {plan.Mode}, {DistanceTo(target):F0}m to go");
        var startedAt = Environment.TickCount64;
        bool StopCondition()
        {
            Status = label;
            if (WithinReach(target, arriveWithin))
            {
                return true;
            }
            if (!canRemount || !FreeToMount() || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted] || DistanceTo(target) <= MountMinMeters)
            {
                return false;
            }
            remount = true;
            return true;
        }
        bool AbortIfStalled()
        {
            if (Environment.TickCount64 >= nextProgressLogAt)
            {
                nextProgressLogAt = Environment.TickCount64 + TravelProgressLogMs;
                Diag($"{scope}: {DistanceTo(target):F0}m to go, navigating {NavmeshIPC.Instance.IsRunning()}, {ConditionTag()}");
            }
            var stallKind = tracker.Check();
            if (stallKind == StallKind.None)
            {
                return false;
            }
            Diag($"{scope}: stalled ({stallKind}) {DistanceTo(target):F0}m short of the spot");
            return true;
        }
        var operation = new MoveOp(move => move.MoveInZone(plan.Target, MovementFor(plan.Mode, plan.ToleranceFor(arriveWithin)), StopCondition));
        var completed = await RunCancellable(operation, watchdogMs, scope, AbortIfStalled);
        if (remount)
        {
            Diag($"{scope}: free to mount with {DistanceTo(target):F0}m to go; stopping to mount");
            return LegOutcome.Remount;
        }
        var fault = operation.Fault;
        if (fault != null)
        {
            Diag(scope + ": faulted: " + fault.Message);
        }
        var failed = operation.Fault != null || !completed;
        if ((ride & failed) && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return LegOutcome.MountFailed;
        }
        if (operation.Fault != null)
        {
            return LegOutcome.Faulted;
        }
        if (!completed)
        {
            return LegOutcome.Stalled;
        }
        return (Environment.TickCount64 - startedAt < FalseStartSettleMs) ? LegOutcome.FalseStart : LegOutcome.EndedShort;
    }

    private async Task RecoverFromFalseStart(string scope, int falseStarts)
    {
        Diag($"{scope}: the path was dropped within {FalseStartSettleMs}ms of starting ({falseStarts}/{MaxFalseStarts}; {ConditionTag()})");
        CancelDescent();
        if (await WaitForStillness(FalseStartSettleMs) || outsideMovementWarned || CancelToken.IsCancellationRequested)
        {
            return;
        }
        outsideMovementWarned = true;
        Warn(scope + ": the character keeps moving on its own, so every path is dropped as soon as it starts. If the pathfinder's 'Cancel current path on player movement input' setting is on, turn it off; a held movement key, auto-run or a drifting controller does the same.");
        ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] The character keeps moving on its own, so paths are dropped as soon as they start. Turn off the pathfinder's 'cancel path on movement input' setting, and check that no key or auto-run is held.");
    }

    private async Task<bool> WaitForStillness(int budgetMs)
    {
        var deadline = Environment.TickCount64 + budgetMs;
        Vector3? anchor = null;
        var stillSinceMs = Environment.TickCount64;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
            if (localPlayer != null)
            {
                var tickCount = Environment.TickCount64;
                var position = localPlayer.Position;
                if (!anchor.HasValue || Vector3.Distance(anchor.Value, position) > StillnessMeters)
                {
                    anchor = position;
                    stillSinceMs = tickCount;
                }
                else if (tickCount - stillSinceMs >= StillnessConfirmMs)
                {
                    return true;
                }
            }
            await DelayMs(StillnessPollMs);
        }
        return false;
    }

    private async Task<bool> RecoverIfOffMesh(uint territoryId, Vector3 target, string scope)
    {
        if (!StuckDetector.IsOffMesh())
        {
            return false;
        }
        await DelayMs(1000);
        if (CancelToken.IsCancellationRequested || !StuckDetector.IsOffMesh())
        {
            return false;
        }
        var position = ECommons.DalamudServices.Svc.Objects.LocalPlayer?.Position ?? Vector3.Zero;
        Warn($"{scope}: no floor of the world within {5f:F0}m of {FormatPosition(position)} ({ConditionTag()}); the character is off the world's surface, teleporting out");
        return await TryTeleportShortcut(territoryId, target, scope + "-offmesh", TeleportPurpose.OffMesh);
    }

    private async Task<bool> TryTeleportShortcut(uint territoryId, Vector3 target, string label, TeleportPurpose purpose)
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer)
        {
            return false;
        }
        if (!ZoneAetherytes.TryFindNearest(territoryId, target, out var nearest))
        {
            return false;
        }
        var currentDistance = Vector3.Distance(localPlayer.Position, target);
        var aetheryteDistance = Vector3.Distance(nearest.Position, target);
        var hopDistance = Vector3.Distance(localPlayer.Position, nearest.Position);
        if (!(purpose switch
        {
            TeleportPurpose.Shortcut => currentDistance - aetheryteDistance >= TeleportShortcutMinSavingMeters,
            TeleportPurpose.Recovery => hopDistance >= TeleportRecoveryMinHopMeters && aetheryteDistance < currentDistance,
            _ => true,
        }))
        {
            return false;
        }
        Diag(purpose switch
        {
            TeleportPurpose.Shortcut => $"{label}: {nearest.Name} is {aetheryteDistance:F0}m from the spot against {currentDistance:F0}m from here; teleporting",
            TeleportPurpose.Recovery => $"{label}: teleporting to {nearest.Name} to get unstuck, {aetheryteDistance:F0}m from the spot",
            _ => $"{label}: teleporting to {nearest.Name} to get back onto the world, {aetheryteDistance:F0}m from the spot",
        });
        return await TeleportToAetheryte(territoryId, nearest, label);
    }

    private async Task<bool> TryTeleportToConnectedAetheryte(uint territoryId, Vector3 target, float reachMeters, string scope)
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer)
        {
            return false;
        }
        var here = localPlayer.Position;
        var aetherytes = ZoneAetherytes.TeleportableIn(territoryId);
        var navmesh = NavmeshIPC.Instance;
        ZoneAetheryte? connected = null;
        var shortest = float.MaxValue;
        for (var index = 0; index < aetherytes.Length; index++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            var aetheryte = aetherytes.Span[index];
            if (!ZoneAetherytes.IsAttuned(aetheryte.Id) || Vector3.Distance(here, aetheryte.Position) < 50f)
            {
                continue;
            }
            if (navmesh.NearestStandablePoint(aetheryte.Position, AetheryteFloorHalfExtentMeters, AetheryteFloorHalfExtentMeters) is not { } standablePoint)
            {
                continue;
            }
            var groundRoute = await ProbeGroundRoute(standablePoint, target);
            var connectsToSpot = groundRoute.Kind == GroundRouteKind.Complete || (groundRoute.Kind == GroundRouteKind.Partial && groundRoute.ShortfallMeters <= reachMeters);
            Diag($"{scope}: from {aetheryte.Name} the ground route is {groundRoute.Kind}, {groundRoute.LengthMeters:F0}m long and {groundRoute.ShortfallMeters:F0}m short of the spot");
            if (!connectsToSpot || groundRoute.LengthMeters >= shortest)
            {
                continue;
            }
            shortest = groundRoute.LengthMeters;
            connected = aetheryte;
        }
        ZoneAetheryte bestAetheryte;
        if (connected.HasValue)
        {
            bestAetheryte = connected.GetValueOrDefault();
        }
        else
        {
            return false;
        }
        Diag($"{scope}: the spot is on another landmass; {bestAetheryte.Name} reaches it over {shortest:F0}m of ground, teleporting there");
        return await TeleportToAetheryte(territoryId, bestAetheryte, scope + "-landmass");
    }

    private async Task<bool> TeleportToAetheryte(uint territoryId, ZoneAetheryte aetheryte, string label)
    {
        var moved = false;
        await RunWithStatusPinned("Teleporting to " + aetheryte.Name, async () =>
        {
            await PrepareForTeleport(label);
            if (CancelToken.IsCancellationRequested || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer)
            {
                return;
            }
            var before = localPlayer.Position;
            var operation = new MoveOp(move => move.Teleport(territoryId, aetheryte.Position, allowSameZoneTeleport: true));
            await RunCancellable(operation, TravelTeleportWatchdogMs, label, StuckDetector.IdleStallAbort(8000));
            var fault = operation.Fault;
            if (fault != null)
            {
                Diag(label + ": teleport faulted: " + fault.Message);
                return;
            }
            moved = ECommons.DalamudServices.Svc.Objects.LocalPlayer is { } localPlayerAfter && Vector3.Distance(before, localPlayerAfter.Position) >= TeleportMovedMeters;
        });
        if (!moved)
        {
            Diag(label + ": the teleport to " + aetheryte.Name + " did not move the character; carrying on from here");
            return false;
        }
        await WaitForNavmeshReady(TravelNavmeshWaitMs, TravelNavmeshPollFrames);
        return true;
    }

    private async Task TryAethernetShortcut(uint territoryId, Vector3 target)
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer)
        {
            return;
        }
        if (!ZoneAetherytes.TryFindAethernetShortcut(territoryId, localPlayer.Position, target, out var shortcut))
        {
            return;
        }
        Diag($"Travel: riding the aethernet from {shortcut.Source.Name} to {shortcut.Destination.Name}, about {shortcut.SavedMeters:F0}m shorter than walking");
        await RunWithStatusPinned("Riding the aethernet to " + shortcut.Destination.Name, async () =>
        {
            var operation = new MoveOp(move => move.Aethernet(territoryId, shortcut.Destination.Position));
            await RunCancellable(operation, AethernetLegMs, "travel-aethernet");
            var fault = operation.Fault;
            if (fault != null)
            {
                Diag("Travel: the aethernet ride faulted: " + fault.Message + "; walking instead");
            }
        });
        await WaitForNavmeshReady(TravelNavmeshWaitMs, TravelNavmeshPollFrames);
    }

    private async Task Unstick(string scope, Vector3 target, int stalls)
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            Diag(scope + ": stuck in the air (" + ConditionTag() + ")");
            await BackOffInFlight(scope);
            return;
        }
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.Swimming] || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Diving])
        {
            Diag(scope + ": stuck in the water (" + ConditionTag() + "); re-pathing");
            return;
        }
        Status = "Getting unstuck";
        Diag(scope + ": stuck (" + ConditionTag() + "); jumping and stepping off the snag");
        UseGeneralAction(JumpGeneralActionId);
        await DelayMs(UnstickSettleMs);
        if (CancelToken.IsCancellationRequested || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer)
        {
            return;
        }
        var position = localPlayer.Position;
        var navmesh = NavmeshIPC.Instance;
        var standable = navmesh.NearestStandablePoint(position, UnstickStepHalfExtentMeters, UnstickStepHalfExtentMeters);
        if (!standable.HasValue || Vector3.Distance(position, standable.Value) < UnstickStepMinMeters)
        {
            standable = SideStep(navmesh, position, target, stalls);
        }
        if (standable is not { } destination)
        {
            return;
        }
        var stepLabel = scope + "-step";
        Diag($"{stepLabel}: stepping {Vector3.Distance(position, destination):F1}m to {FormatPosition(destination)}");
        var operation = new MoveOp(move => move.MoveInZone(destination, walkMovement.WithTolerance(UnstickStepToleranceMeters), null));
        await RunCancellable(operation, UnstickStepWatchdogMs, stepLabel, StuckDetector.MoveStallAbort(stepLabel));
    }

    private static Vector3? SideStep(NavmeshIPC navmesh, Vector3 position, Vector3 target, int stalls)
    {
        var direction = new Vector2(target.X - position.X, target.Z - position.Z);
        if (direction.LengthSquared() < 2.25f)
        {
            return null;
        }
        direction = Vector2.Normalize(direction);
        var perpendicular = ((stalls % 2 == 0) ? new Vector2(-direction.Y, direction.X) : new Vector2(direction.Y, -direction.X));
        var sidePoint = position + new Vector3(perpendicular.X * UnstickSideStepMeters, 0f, perpendicular.Y * UnstickSideStepMeters);
        var standable = navmesh.NearestStandablePoint(sidePoint, UnstickSideStepHalfExtentMeters, UnstickSideStepHalfExtentMeters);
        if (standable is not { } standablePoint || !(Vector3.Distance(position, standablePoint) >= UnstickStepMinMeters))
        {
            return null;
        }
        return standablePoint;
    }

    private Vector3 SnapToFloor(Vector3 destination)
    {
        var navmesh = NavmeshIPC.Instance;
        var point = destination with { Y = destination.Y + SnapLiftMeters };
        var floor = navmesh.PointOnFloor(point, allowUnlandable: false, SnapHalfExtentMeters) ?? navmesh.PointOnFloor(point, allowUnlandable: true, SnapHalfExtentMeters);
        var snapped = (floor is { } floorPoint && destination.Y - floorPoint.Y <= SnapMaxDropMeters) ? floorPoint : (navmesh.NearestStandablePoint(destination) ?? destination);
        var snapDistance = Vector3.Distance(destination, snapped);
        if (snapDistance >= SnapReportMeters)
        {
            Diag($"Travel: destination snapped {snapDistance:F1}m onto the floor at {FormatPosition(snapped)}");
        }
        return snapped;
    }

    private static bool PlayerReady() => ECommons.DalamudServices.Svc.Objects.LocalPlayer != null && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas51];

    private static bool FreeToMount() => !ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Swimming] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Diving];

    private static bool Arrived(Vector3 target, Vector3 destination, float arriveWithin) => WithinReach(target, arriveWithin) || WithinReach(destination, arriveWithin);

    private static float DistanceTo(Vector3 target)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return float.MaxValue;
        }
        return Vector3.Distance(localPlayer.Position, target);
    }

    private static int TravelBudgetMs(Vector3 target)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        var travelEstimateMs = ((localPlayer != null) ? Vector3.Distance(localPlayer.Position, target) : 0f) / TravelMinSpeedMetersPerSecond * 1000f;
        return (int)Math.Min(TravelMaxBudgetMs, TravelBaseBudgetMs + travelEstimateMs);
    }

    private static bool TerritoryAllowsMount(uint territoryId) => ECommons.DalamudServices.Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryId)?.Mount ?? false;

    private static string FormatPosition(Vector3 position) => $"({position.X:F1}, {position.Y:F1}, {position.Z:F1})";
}
