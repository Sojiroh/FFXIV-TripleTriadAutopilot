using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const float EntranceApproachMeters = 3f;

    private const float EntranceInteractMeters = 3f;

    private const float EntranceWalkToleranceMeters = 0.5f;

    private const int EntranceWalkWatchdogMs = 10000;

    private const int EntranceTimeoutMs = 30000;

    private async Task<bool> EnterThroughEntrance(EntranceRoute route)
    {
        var targetName = TerritoryNames.Of(route.TargetTerritoryId);
        Diag($"Travel: entering {targetName} ({route.TargetTerritoryId}) through entrance {route.EntranceBaseId} in territory {route.SourceTerritoryId}");
        try
        {
            var failed = !await TravelTo(route.SourceTerritoryId, route.Position, EntranceApproachMeters);
            if (!failed)
            {
                failed = !await SafeDismount("entrance");
            }
            if (failed || CancelToken.IsCancellationRequested)
            {
                return false;
            }
            NavmeshIPC.Instance.Stop();
            return await InteractWithEntrance(route, targetName);
        }
        finally
        {
            NavmeshIPC.Instance.Stop();
        }
    }

    private async Task<bool> InteractWithEntrance(EntranceRoute route, string targetName)
    {
        Status = "Entering " + targetName;
        var deadline = Environment.TickCount64 + EntranceTimeoutMs;
        var interacted = false;
        var walkedUp = false;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            var territoryType = ECommons.DalamudServices.Svc.ClientState.TerritoryType;
            if (territoryType == route.TargetTerritoryId)
            {
                Diag($"Travel: entered {targetName} ({route.TargetTerritoryId})");
                return await WaitForPlayerReady();
            }
            if (territoryType != route.SourceTerritoryId)
            {
                Warn($"Travel: entered territory {territoryType} instead of {targetName} ({route.TargetTerritoryId})");
                return false;
            }
            if (interacted)
            {
                DialogDriver.AdvanceAccepting();
            }
            else if (!DialogDriver.AnyOpen() && NpcInteraction.PlayerReady())
            {
                var entrance = NpcInteraction.FindNearest(route.EntranceBaseId);
                if (entrance != null)
                {
                    var position = entrance.Position;
                    if (WithinEntranceReach(position))
                    {
                        NpcInteraction.Target(entrance);
                        NpcInteraction.Interact(entrance);
                        interacted = true;
                        Diag($"Travel: interacted with entrance {route.EntranceBaseId}; waiting for territory {route.TargetTerritoryId}");
                    }
                    else
                    {
                        if (walkedUp)
                        {
                            Warn($"Travel: entrance {route.EntranceBaseId} is still beyond {EntranceInteractMeters:F0}m after walking up to it");
                            return false;
                        }
                        walkedUp = true;
                        await WalkUpToEntrance(position);
                    }
                }
            }
            await NextFrame(5);
        }
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }
        if (interacted)
        {
            await SettleDialog();
        }
        var entranceFound = NpcInteraction.FindNearest(route.EntranceBaseId) is not null;
        Warn($"Travel: timed out entering {targetName} ({route.TargetTerritoryId}); still in territory {ECommons.DalamudServices.Svc.ClientState.TerritoryType}, entrance found: {entranceFound}, interacted: {interacted}, blockers: {NpcInteraction.DescribeBlockers()}");
        return false;
    }

    private async Task WalkUpToEntrance(Vector3 entrancePosition)
    {
        var walk = new MoveOp(move => move.MoveInZone(entrancePosition, walkMovement.WithTolerance(EntranceWalkToleranceMeters), () => WithinEntranceReach(entrancePosition)));
        await RunCancellable(walk, EntranceWalkWatchdogMs, "entrance-walk", StuckDetector.MoveStallAbort("entrance-walk"));
        var fault = walk.Fault;
        if (fault != null)
        {
            Diag("Travel: the walk up to the entrance faulted: " + fault.Message);
        }
    }

    private static bool WithinEntranceReach(Vector3 entrancePosition)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer != null)
        {
            return Vector3.Distance(localPlayer.Position, entrancePosition) <= EntranceInteractMeters;
        }
        return false;
    }
}
