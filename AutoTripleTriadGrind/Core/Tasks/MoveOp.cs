using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;
using clib.Enums;
using clib.Extensions;
using clib.Services;
using clib.TaskSystem;
using clib.Utils;

namespace AutoTripleTriadGrind.Core.Tasks;

internal sealed class MoveOp(Func<MoveOp, Task> body) : AutoTask
{
    public Exception? Fault { get; private set; }

    protected override async Task Execute()
    {
        try
        {
            await body(this);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception fault)
        {
            Fault = fault;
        }
    }

    private const float DefaultArriveMeters = 3f;

    private const float TeleportCastCompleteSeconds = 5f;

    private const uint DismountGeneralActionId = 23u;

    private const uint FlyingMountRouletteGeneralActionId = 24u;

    private const int InteractAttempts = 5;

    private const string SelectStringAddon = "SelectString";

    private const string AethernetAddon = "TelepotTown";

    private const string TalkAddon = "Talk";

    private static NavmeshIPC Navmesh => NavmeshIPC.Instance;

    private static IPlayerCharacter? Player => ECommons.DalamudServices.Svc.Objects.LocalPlayer;

    public Task MoveInZone(Vector3 destination, MovementConfig config, Func<bool>? stopCondition)
    {
        return Navigate(destination, config, stopCondition);
    }

    public Task Teleport(uint territoryId, Vector3 destination, bool allowSameZoneTeleport)
    {
        return TeleportToTerritory(territoryId, destination, allowSameZoneTeleport);
    }

    public Task Aethernet(uint territoryId, Vector3 destination)
    {
        return UseAethernet(territoryId, destination);
    }

    public Task Interact(IGameObject gameObject, Func<bool>? waitUntil, UiSkipOptions skip)
    {
        return InteractWith(gameObject, waitUntil, skip);
    }

    public Task DismountNow()
    {
        return Dismount();
    }

    private async Task Navigate(Vector3 destination, MovementConfig config, Func<bool>? stopCondition)
    {
        await WaitUntil(() => IPlayerCharacterExtensions.get_Available(Player), "WaitingForPlayer");
        var tolerance = Math.Max(config.Tolerance.GetValueOrDefault(), Navmesh.Tolerance());
        if (Player.WithinRange(destination, tolerance) || (stopCondition?.Invoke() ?? false))
        {
            return;
        }
        if (config.Movement.HasFlag(MovementOptions.Mount) || config.Movement.HasFlag(MovementOptions.Fly))
        {
            await Mount();
        }
        await WaitUntil(() => Navmesh.IsReady() || Navmesh.BuildProgress() >= 0f, "WaitingForNavmesh");
        await WaitWhile(() => Navmesh.BuildProgress() >= 0f, "BuildingNavmesh");
        ErrorIf(!Navmesh.IsReady(), "Could not load the navmesh for this zone");
        await WaitWhile(Navmesh.IsPathfinding, "WaitingForPathfinding");
        CancelToken.ThrowIfCancellationRequested();
        using (new OnDispose(StopOwnedPath))
        {
            var fly = IPlayerCharacterExtensions.get_InFlight(Player) || (config.Movement.HasFlag(MovementOptions.Fly) && Control.CanFly);
            ErrorIf(!Navmesh.PathfindAndMoveCloseTo(destination, fly, config.Tolerance ?? DefaultArriveMeters), $"Could not start pathfinding to {destination}");
            Status = $"Moving to {destination}";
            await NextFrame();
            await WaitWhile(() =>
            {
                if (!Player.WithinRange(destination, tolerance))
                {
                    if (stopCondition?.Invoke() != true)
                    {
                        return Navmesh.IsBusy();
                    }
                }
                return false;
            }, "Navigate");
            if (stopCondition?.Invoke() ?? false)
            {
                await StopPathfinding();
            }
        }
        if (config.Movement.HasFlag(MovementOptions.Dismount) && Player.WithinRange(destination, tolerance))
        {
            await Dismount();
        }
    }

    private async Task TeleportToTerritory(uint territoryId, Vector3 destination, bool allowSameZoneTeleport)
    {
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId && !allowSameZoneTeleport)
        {
            return;
        }
        await StopPathfinding();
        await WaitWhile(() => IPlayerCharacterExtensions.get_IsUiFading(Player), "WaitingForUi");
        var aetheryteId = Coords.FindPrimaryAetheryte(Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault());
        ErrorIf(aetheryteId == 0, $"No unlocked aetheryte for territory {territoryId}");
        var row = ECommons.DalamudServices.Svc.Data.GetExcelSheet<Aetheryte>().GetRow(aetheryteId);
        if ((ECommons.DalamudServices.Svc.ClientState.TerritoryType != row.Territory.RowId) | allowSameZoneTeleport)
        {
            Status = $"Teleporting to {row.PlaceName.Value.Name}";
            await CastTeleport(aetheryteId);
        }
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId)
        {
            await UseAethernet(territoryId, destination);
        }
        ErrorIf(ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId, $"Failed to reach territory {territoryId}");
    }

    private async Task CastTeleport(uint aetheryteId)
    {
        var teleported = false;
        while (!teleported)
        {
            await StopPathfinding();
            await WaitWhile(() => IPlayerCharacterExtensions.get_IsBusy(Player), "WaitingForTeleport");
            CancelToken.ThrowIfCancellationRequested();
            ErrorIf(!ActionManagerExtensions.Teleport(aetheryteId, 0), $"Teleport to aetheryte {aetheryteId} was rejected");
            teleported = await WaitForTeleportOutcome();
        }
    }

    private async Task<bool> WaitForTeleportOutcome()
    {
        var sawCast = false;
        var sawFade = false;
        while (true)
        {
            var fading = IPlayerCharacterExtensions.get_IsUiFading(Player);
            var casting = Player?.IsCasting ?? false;
            sawCast |= casting;
            sawFade |= fading;
            if (sawFade && !fading)
            {
                await WaitUntil(() => GameMainExtensions.get_IsTerritoryLoaded() && IPlayerCharacterExtensions.get_Interactable(Player), "TeleportArrival");
                return true;
            }
            if (sawCast && !casting && !sawFade && ActionManagerExtensions.GetCastAction().Elapsed != TeleportCastCompleteSeconds)
            {
                return false;
            }
            await NextFrame();
        }
    }

    private async Task UseAethernet(uint territoryId, Vector3 destination)
    {
        var source = Coords.FindClosestAetheryte(ECommons.DalamudServices.Svc.ClientState.TerritoryType, Player!.Position).GetValueOrDefault();
        var target = Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault();
        if (source == 0 || target == 0 || source == target)
        {
            return;
        }
        var primaryAetheryte = Coords.FindPrimaryAetheryte(source);
        if (primaryAetheryte == 0 || primaryAetheryte != Coords.FindPrimaryAetheryte(target))
        {
            return;
        }
        if (Coords.FindAetheryte(source).id == 0L)
        {
            source = primaryAetheryte;
        }
        Status = $"Riding the aethernet to territory {territoryId}";
        await ApproachAetheryte(source);
        var isAetheryte = ECommons.DalamudServices.Svc.Data.GetExcelSheet<Aetheryte>().GetRow(source).IsAetheryte;
        await WaitForTravelMenu(isAetheryte ? SelectStringAddon : AethernetAddon);
        PacketDispatcherExtensions.TeleportToAethernet(source, target);
        await WaitUntilThenFalse(() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas], "AethernetTransition");
        await WaitUntil(() => ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId && GameMainExtensions.get_IsTerritoryLoaded() && IPlayerCharacterExtensions.get_Interactable(Player), "AethernetArrival");
    }

    private async Task ApproachAetheryte(uint aetheryteId)
    {
        var (objectId, aetherytePosition) = Coords.FindAetheryte(aetheryteId);
        ErrorIf(objectId == 0, $"Aetheryte {aetheryteId} is not nearby");
        if (!Player.WithinRange(aetherytePosition, InteractRange.Aetheryte.MaxDistance))
        {
            await Navigate(aetherytePosition, MovementConfig.Default.WithTolerance(InteractRange.Aetheryte), null);
        }
        if (IPlayerCharacterExtensions.get_Mounted(Player))
        {
            await Dismount();
        }
        CancelToken.ThrowIfCancellationRequested();
        ErrorIf(!TargetSystemExtensions.InteractWith(objectId), $"Could not interact with aetheryte {aetheryteId}");
    }

    private async Task WaitForTravelMenu(string addonName)
    {
        while (!AddonReady(addonName))
        {
            CancelToken.ThrowIfCancellationRequested();
            AddonTalkExtensions.Progress();
            await NextFrame();
        }
    }

    private async Task InteractWith(IGameObject gameObject, Func<bool>? waitUntil, UiSkipOptions skip)
    {
        if (!gameObject.IsInInteractRange())
        {
            await Navigate(gameObject.Position, MovementConfig.Default.WithTolerance(InteractRange.EventObj), gameObject.IsInInteractRange);
        }
        Status = $"Interacting with {gameObject.Name}";
        await WaitWhile(() => IPlayerCharacterExtensions.get_IsJumping(Player), "WaitingToInteract");
        for (var attempt = 0; attempt < InteractAttempts; attempt++)
        {
            if (TargetSystemExtensions.InteractWith(gameObject.GameObjectId))
            {
                if (waitUntil != null)
                {
                    await WaitUntilSkipping(waitUntil, skip);
                }
                return;
            }
            await NextFrame();
        }
        Error($"Could not interact with {gameObject.Name} after {InteractAttempts} tries");
    }

    private async Task WaitUntilSkipping(Func<bool> condition, UiSkipOptions skip)
    {
        while (!condition())
        {
            CancelToken.ThrowIfCancellationRequested();
            if (skip.HasFlag(UiSkipOptions.Talk) && AddonReady(TalkAddon))
            {
                AddonTalkExtensions.Progress();
            }
            await NextFrame();
        }
    }

    private async Task Mount()
    {
        if (!IPlayerCharacterExtensions.get_CanMount(Player))
        {
            return;
        }
        Status = "Mounting";
        while (!IPlayerCharacterExtensions.get_Mounted(Player))
        {
            CancelToken.ThrowIfCancellationRequested();
            if (!IPlayerCharacterExtensions.get_IsBusy(Player) && !ActionManagerExtensions.IsActionInUse(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId))
            {
                ActionManagerExtensions.UseAction(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId);
            }
            await NextFrame();
        }
    }

    private async Task Dismount()
    {
        if (Player == null || !IPlayerCharacterExtensions.get_Mounted(Player))
        {
            return;
        }
        if (IPlayerCharacterExtensions.get_InFlight(Player))
        {
            if (Navmesh.NearestPointReachable(Player.Position) is { } landingPoint)
            {
                await Navigate(landingPoint, MovementConfig.Everything, null);
            }
        }
        Status = "Dismounting";
        while (IPlayerCharacterExtensions.get_Mounted(Player))
        {
            CancelToken.ThrowIfCancellationRequested();
            if (IPlayerCharacterExtensions.get_InFlight(Player) && !IPlayerCharacterExtensions.get_IsAirDismountable(Player))
            {
                ActionManagerExtensions.UseAction(ActionType.GeneralAction, DismountGeneralActionId);
            }
            else if (IPlayerCharacterExtensions.get_InFlight(Player))
            {
                GameMainExtensions.ExecuteLocationCommand(LocationCommandFlag.Dismount, Player.Position, (int)IPlayerCharacterExtensions.get_PackedRotation(Player));
            }
            else
            {
                GameMainExtensions.ExecuteCommand(CommandFlag.Dismount, 1);
            }
            await NextFrame();
        }
    }

    private async Task StopPathfinding()
    {
        CancelToken.ThrowIfCancellationRequested();
        Navmesh.Stop();
        if (!Navmesh.IsPathfinding())
        {
            return;
        }
        await WaitWhile(Navmesh.IsPathfinding, "StopPathfinding");
        Navmesh.Stop();
    }

    private static void StopOwnedPath()
    {
        NavmeshIPC.Instance.Stop();
    }

    private static bool AddonReady(string addonName)
    {
        return AtkUnitBaseExtensions.IsAddonReady(addonName);
    }
}

internal enum StallKind
{
    None,
    NavWedge,
    Idle
}
