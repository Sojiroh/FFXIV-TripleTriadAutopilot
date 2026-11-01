using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Travel;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using clib.TaskSystem;

namespace AutoTripleTriadGrind.Core.Tasks;

public sealed class AutoReturnToInn(string postRunCommands) : AutoCommon
{
    private const float InnkeeperArrivalMeters = 3.5f;

    private const float InnkeeperReachMeters = 5f;

    private const float InnkeeperStepToleranceMeters = 2.5f;

    private const int EnterInnTimeoutMs = 60000;

    private const int ApproachWatchdogMs = 25000;

    private const int InteractWatchdogMs = 8000;

    private const int DialogPollMs = 250;

    private const int InteractRetryMs = 500;

    private readonly string postRunCommands = postRunCommands;

    protected override async Task Execute()
    {
        await SendPostRunCommandsWhenSafe();
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        var inn = GrandCompanyInns.ForPlayer();
        var cityName = TerritoryNames.Of(inn.CityTerritoryId);
        Diag($"Return to inn: {cityName} ({inn.CityTerritoryId}), innkeeper {inn.InnkeeperBaseId}, inn room territory {inn.InnTerritoryId}.");
        if (Svc.ClientState.TerritoryType == inn.InnTerritoryId)
        {
            Diag("Return to inn: already inside the inn room.");
            return;
        }
        Svc.Chat.Print("[TTA] Run complete. Retiring to the inn in " + cityName + ".");
        try
        {
            var reachedCity = await TravelTo(inn.CityTerritoryId, inn.InnkeeperPosition, InnkeeperArrivalMeters);
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (!reachedCity && Svc.ClientState.TerritoryType != inn.CityTerritoryId)
            {
                Warn("Return to inn: could not reach " + cityName + "; staying where the run ended.");
                return;
            }
            if (Svc.Condition[ConditionFlag.Mounted])
            {
                await SafeDismount("inn-dismount");
            }
            await EnterInn(inn, cityName);
        }
        finally
        {
            NavmeshIPC.Instance.Stop();
        }
    }

    private async Task SendPostRunCommandsWhenSafe()
    {
        if (string.IsNullOrWhiteSpace(postRunCommands))
        {
            return;
        }
        if (await WaitUntilSafeToFinish("inn-commands-ready"))
        {
            await SendPostRunCommands(postRunCommands);
            return;
        }
        if (!CancelToken.IsCancellationRequested)
        {
            Warn($"Return to inn: chat commands skipped, no safe moment (combat, casting, or a zone change) within {20}s.");
        }
    }

    private async Task EnterInn(CityInn inn, string cityName)
    {
        Status = "Entering the inn in " + cityName;
        var deadline = Environment.TickCount64 + EnterInnTimeoutMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (Svc.ClientState.TerritoryType == inn.InnTerritoryId)
            {
                Diag("Return to inn: reached the inn room.");
                return;
            }
            if (DialogDriver.AdvanceAccepting())
            {
                await DelayMs(DialogPollMs);
                continue;
            }
            var innkeeper = FindByBaseId(inn.InnkeeperBaseId);
            if (innkeeper == null || Svc.Objects.LocalPlayer is not { } localPlayer)
            {
                await DelayMs(DialogPollMs);
                continue;
            }
            var position = innkeeper.Position;
            var distance = Vector3.Distance(localPlayer.Position, position);
            if (distance > InnkeeperReachMeters)
            {
                await ApproachInnkeeper(position, distance);
            }
            else
            {
                await TalkToInnkeeper(innkeeper);
            }
        }
        if (!CancelToken.IsCancellationRequested)
        {
            Warn("Return to inn: timed out before entering the inn room.");
        }
    }

    private async Task ApproachInnkeeper(Vector3 innkeeperPosition, float distance)
    {
        Diag($"Return to inn: walking up to the innkeeper, {distance:F1}m away.");
        var approach = new MoveOp(move => move.MoveInZone(innkeeperPosition, MovementConfig.Default.WithTolerance(InnkeeperStepToleranceMeters), null));
        await RunCancellable(approach, ApproachWatchdogMs, "inn-approach", StuckDetector.MoveStallAbort("inn-approach"));
        var fault = approach.Fault;
        if (fault != null)
        {
            Diag("Return to inn: the walk to the innkeeper faulted: " + fault.Message + "; retrying");
        }
    }

    private async Task TalkToInnkeeper(IGameObject innkeeper)
    {
        Diag("Return to inn: talking to the innkeeper.");
        var interact = new MoveOp(move => move.Interact(innkeeper, DialogDriver.AnyOpen, UiSkipOptions.Talk));
        await RunCancellable(interact, InteractWatchdogMs, "inn-interact");
        var fault = interact.Fault;
        if (fault != null)
        {
            Diag("Return to inn: talking to the innkeeper failed: " + fault.Message + "; retrying");
        }
        await DelayMs(InteractRetryMs);
    }

    private static IGameObject? FindByBaseId(uint baseId)
    {
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            var gameObject = objects[i];
            if (gameObject != null && gameObject.BaseId == baseId)
            {
                return gameObject;
            }
        }
        return null;
    }
}
