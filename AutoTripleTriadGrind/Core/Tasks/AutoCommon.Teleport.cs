using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using clib.TaskSystem;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int TeleportCastClearMs = 10000;

    private const int GroundingMoveMs = 20000;

    private const int ZoneLoadSettleMs = 5000;

    private const int TeleportRetryBackoffMs = 2000;

    private const int ReturnHomeWaitMs = 30000;

    private const int ReturnHomeReissueMs = 3000;

    private const int ReturnHomePollMs = 250;

    private const uint ReturnGeneralActionId = 8u;

    private const int MaxTeleportFaults = 2;

    private const int StallsBeforeReturnHome = 2;

    private const int GatewayAttempts = 3;

    private const int AethernetLegMs = 90000;

    private const int AethernetNavmeshWaitMs = 60000;

    private const int AethernetNavmeshPollFrames = 60;

    private const float GroundSearchHalfExtentMeters = 30f;

    private const float GroundedEnoughMeters = 2f;

    private const float GroundArrivalToleranceMeters = 3f;

    private static readonly ConditionTagRule[] conditionTagRules = new ConditionTagRule[]
    {
        new ConditionTagRule("combat", new ConditionFlag[] { ConditionFlag.InCombat }),
        new ConditionTagRule("cast", new ConditionFlag[]
        {
            ConditionFlag.Casting,
            ConditionFlag.Casting87
        }),
        new ConditionTagRule("mount", new ConditionFlag[]
        {
            ConditionFlag.Mounted,
            ConditionFlag.RidingPillion
        }),
        new ConditionTagRule("flight", new ConditionFlag[] { ConditionFlag.InFlight }),
        new ConditionTagRule("dive", new ConditionFlag[] { ConditionFlag.Diving }),
        new ConditionTagRule("swim", new ConditionFlag[] { ConditionFlag.Swimming }),
        new ConditionTagRule("jump", new ConditionFlag[]
        {
            ConditionFlag.Jumping,
            ConditionFlag.Jumping61
        }),
        new ConditionTagRule("moved", new ConditionFlag[] { ConditionFlag.BeingMoved }),
        new ConditionTagRule("zoning", new ConditionFlag[]
        {
            ConditionFlag.BetweenAreas,
            ConditionFlag.BetweenAreas51
        }),
        new ConditionTagRule("occupied", new ConditionFlag[]
        {
            ConditionFlag.Occupied33,
            ConditionFlag.Occupied38,
            ConditionFlag.Occupied39
        })
    };

    internal static string ConditionTag()
    {
        var condition = ECommons.DalamudServices.Svc.Condition;
        var tags = new StringBuilder();
        for (var i = 0; i < conditionTagRules.Length; i++)
        {
            var rule = conditionTagRules[i];
            if (AnyFlagSet(condition, rule.Flags))
            {
                if (tags.Length > 0)
                {
                    tags.Append(',');
                }
                tags.Append(rule.Tag);
            }
        }
        if (tags.Length != 0)
        {
            return tags.ToString();
        }
        return "grounded";
    }

    internal async Task PrepareForTeleport(string scope)
    {
        NavmeshIPC.Instance.Stop();
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting])
        {
            Status = "Waiting for the cast to end before teleporting";
            Diag($"{scope}: casting ({ConditionTag()}), waiting up to {TeleportCastClearMs / 1000}s for it to end");
            await WaitUntilTimed(() => !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting], TeleportCastClearMs, scope + "-wait-castable");
        }
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        if (NotOnSolidGround())
        {
            await GroundForTeleport(scope);
        }
        if (CancelToken.IsCancellationRequested || !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return;
        }
        Diag(scope + ": dismounting before teleport (" + ConditionTag() + ")");
        await SafeDismount(scope + "-dismount");
    }

    internal async Task<bool> TryReturnHome(string scope)
    {
        var startTerritory = ECommons.DalamudServices.Svc.ClientState.TerritoryType;
        Diag(scope + ": teleport not starting (" + ConditionTag() + "); casting Return to the home aetheryte to clear a stuck teleport");
        Status = "Returning home to clear a stuck teleport";
        var deadline = Environment.TickCount64 + ReturnHomeWaitMs;
        var nextCastAt = 0L;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != startTerritory)
            {
                await DelayMs(ZoneLoadSettleMs);
                return true;
            }
            if (Environment.TickCount64 >= nextCastAt && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas51])
            {
                UseGeneralAction(ReturnGeneralActionId);
                nextCastAt = Environment.TickCount64 + ReturnHomeReissueMs;
            }
            await DelayMs(ReturnHomePollMs);
        }
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != startTerritory)
        {
            return true;
        }
        Diag($"{scope}: Return home did not complete within {ReturnHomeWaitMs / 1000}s");
        return false;
    }

    internal async Task<bool> TeleportToTerritory(uint territoryId, Vector3 destination, string label, int perAttemptTimeoutMs, int attempts = 4)
    {
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
        {
            return true;
        }
        var territoryName = TerritoryNames.Of(territoryId);
        var viaGateway = ZoneAetherytes.TryFindGateway(territoryId, out var gateway);
        uint hopTerritoryId;
        Vector3 hopDestination;
        if (viaGateway)
        {
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != gateway.TerritoryId && !ZoneAetherytes.IsAttuned(gateway.AetheryteId))
            {
                Warn($"{label}: {gateway.Name}, the way into {territoryName}, is not attuned; giving up");
                return false;
            }
            if (ZoneAetherytes.HasAethernetStops(territoryId) && !ZoneAetherytes.TryFindEntryNode(territoryId, destination, out var _))
            {
                Warn($"{label}: no aethernet stop of {gateway.Name} into {territoryName} is attuned; attune one first; giving up");
                return false;
            }
            hopTerritoryId = gateway.TerritoryId;
            hopDestination = gateway.Position;
            Diag($"{label}: {territoryName} owns no aetheryte; going through {gateway.Name} and its aethernet");
        }
        else
        {
            if (ZoneAetherytes.TryFindNearest(territoryId, destination, out var nearest))
            {
                hopTerritoryId = territoryId;
                hopDestination = nearest.Position;
                Diag($"{label}: {nearest.Name} is the attuned aetheryte nearest the destination in {territoryName}");
            }
            else
            {
                Warn((ZoneAetherytes.AttunableIdsIn(territoryId).Length == 0) ? $"{label}: {territoryName} ({territoryId}) has no aetheryte to teleport to; giving up" : $"{label}: no aetheryte in {territoryName} ({territoryId}) is attuned; giving up");
                return false;
            }
        }
        var maxAttempts = (viaGateway ? Math.Min(attempts, GatewayAttempts) : attempts);
        var returnedHome = false;
        var stalls = 0;
        var faults = 0;
        for (var attempt = 1; attempt <= maxAttempts && !CancelToken.IsCancellationRequested; attempt++)
        {
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
            {
                return true;
            }
            var scope = $"{label}#{attempt}";
            await PrepareForTeleport(scope);
            if (CancelToken.IsCancellationRequested)
            {
                break;
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != hopTerritoryId)
            {
                var operation = new MoveOp(move => move.Teleport(hopTerritoryId, hopDestination, allowSameZoneTeleport: false));
                var completed = await RunCancellable(operation, perAttemptTimeoutMs, scope, StuckDetector.IdleStallAbort(8000));
                var fault = operation.Fault;
                if (fault != null)
                {
                    faults++;
                    Diag($"{scope} teleport faulted ({faults}/{MaxTeleportFaults}): {fault.Message}");
                }
                else if (!completed)
                {
                    stalls++;
                }
            }
            if (viaGateway && ECommons.DalamudServices.Svc.ClientState.TerritoryType == hopTerritoryId)
            {
                await RideAethernetInto(territoryId, destination, gateway, scope);
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
            {
                return true;
            }
            if (faults >= MaxTeleportFaults)
            {
                Diag($"{label}: teleport rejected {faults} times, not a stuck cast; giving up without Return");
                break;
            }
            if (!returnedHome && stalls >= StallsBeforeReturnHome)
            {
                returnedHome = true;
                await TryReturnHome(label);
            }
            else
            {
                await DelayMs(TeleportRetryBackoffMs);
            }
        }
        return ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId;
    }

    private static bool NotOnSolidGround() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight] || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Diving] || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Swimming];

    private static bool AnyFlagSet(ICondition condition, ConditionFlag[] flags)
    {
        for (var i = 0; i < flags.Length; i++)
        {
            if (condition[flags[i]])
            {
                return true;
            }
        }
        return false;
    }

    private unsafe static void UseGeneralAction(uint generalActionId)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return;
        }
        actionManager->UseAction(ActionType.GeneralAction, generalActionId, 3758096384uL, 0u, ActionManager.UseActionMode.None, 0u, null);
    }

    private async Task GroundForTeleport(string scope)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        var position = localPlayer.Position;
        var navmesh = NavmeshIPC.Instance;
        var groundPoint = (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight] ? (navmesh.PointOnFloor(position, allowUnlandable: false, GroundSearchHalfExtentMeters) ?? navmesh.NearestStandablePoint(position, GroundSearchHalfExtentMeters, GroundSearchHalfExtentMeters)) : navmesh.NearestStandablePoint(position, GroundSearchHalfExtentMeters, GroundSearchHalfExtentMeters));
        if (groundPoint is not { } solidGround)
        {
            Warn(scope + ": off solid ground (" + ConditionTag() + ") with no reachable mesh point to relocate to; teleport may fail");
            return;
        }
        var distance = Vector3.Distance(position, solidGround);
        if (distance < GroundedEnoughMeters)
        {
            return;
        }
        Status = "Returning to solid ground before teleporting";
        Diag($"{scope}: off solid ground ({ConditionTag()}); relocating ~{distance:F0}m to a reachable point before teleport");
        var label = scope + "-ground";
        var operation = new MoveOp(operation => operation.MoveInZone(solidGround, MovementConfig.Everything.WithTolerance(GroundArrivalToleranceMeters), null));
        await RunCancellable(operation, GroundingMoveMs, label, StuckDetector.MoveStallAbort(label));
    }

    private async Task RideAethernetInto(uint territoryId, Vector3 destination, ZoneGateway gateway, string scope)
    {
        Status = "Riding the aethernet from " + gateway.Name;
        await WaitForNavmeshReady(AethernetNavmeshWaitMs, AethernetNavmeshPollFrames);
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        var aim = destination;
        if (ZoneAetherytes.TryFindEntryNode(territoryId, destination, out var entry))
        {
            aim = entry.Position;
            Diag($"{scope}: reached {gateway.Name}; riding its aethernet to {entry.Name} in territory {territoryId}");
        }
        else
        {
            Diag($"{scope}: reached {gateway.Name}; riding its aethernet into territory {territoryId}");
        }
        var operation = new MoveOp(move => move.Aethernet(territoryId, aim));
        await RunCancellable(operation, AethernetLegMs, scope + "-aethernet");
        var fault = operation.Fault;
        if (fault != null)
        {
            Diag(scope + ": aethernet leg faulted: " + fault.Message);
        }
    }

    private readonly record struct ConditionTagRule(string Tag, ConditionFlag[] Flags);
}
