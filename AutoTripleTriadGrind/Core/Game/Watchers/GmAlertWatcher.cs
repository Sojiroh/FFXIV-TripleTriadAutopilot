using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Game.Watchers;

internal sealed class GmAlertWatcher : IDisposable
{
    private const uint GmStatusMin = 1u;

    private const uint GmStatusMax = 3u;

    private const int ScanIntervalMs = 250;

    private const int BeepCountMax = 100;

    private const int BeepFrequencyMinHz = 37;

    private const int BeepFrequencyMaxHz = 32767;

    private const int BeepDurationMaxMs = 5000;

    private const string KillGameCommand = "/xlkill";

    private bool fired;

    private long nextScanAtMs;

    public GmAlertWatcher()
    {
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
    }

    public static void PlayBeeps(int count, int frequencyHz, int durationMs)
    {
        var beeps = Math.Clamp(count, 1, BeepCountMax);
        var frequency = Math.Clamp(frequencyHz, BeepFrequencyMinHz, BeepFrequencyMaxHz);
        var duration = Math.Clamp(durationMs, 1, BeepDurationMaxMs);
        Task.Run(() =>
        {
            for (int i = 0; i < beeps; i++)
            {
                try
                {
                    Console.Beep(frequency, duration);
                }
                catch (Exception ex)
                {
                    RunLog.Debug("Console.Beep failed: " + ex.Message);
                    break;
                }
            }
        });
    }

    private void OnUpdate(IFramework _)
    {
        var tickCount = Environment.TickCount64;
        if (tickCount < nextScanAtMs)
        {
            return;
        }
        nextScanAtMs = tickCount + ScanIntervalMs;
        var configuration = Plugin.Instance.Configuration;
        if (!AnyActionEnabled(configuration) || FindNearbyGm() is not { } nearbyGm)
        {
            fired = false;
            return;
        }
        if (fired)
        {
            return;
        }
        fired = true;
        FireAlerts(configuration, nearbyGm);
    }

    private static IPlayerCharacter? FindNearbyGm()
    {
        var objects = Svc.Objects;
        var localPlayer = objects.LocalPlayer;
        if (localPlayer == null)
        {
            return null;
        }
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] is IPlayerCharacter player && player.EntityId != localPlayer.EntityId)
            {
                var statusId = player.OnlineStatus.RowId;
                if (statusId >= GmStatusMin && statusId <= GmStatusMax)
                {
                    return player;
                }
            }
        }
        return null;
    }

    private static bool AnyActionEnabled(Configuration configuration)
    {
        if (!configuration.GmAlertStopRun && !configuration.GmAlertToast && !configuration.GmAlertChat && !configuration.GmAlertSound && !configuration.GmAlertKillGame)
        {
            return configuration.GmAlertCommands.Count > 0;
        }
        return true;
    }

    private static void FireAlerts(Configuration configuration, IPlayerCharacter gm)
    {
        var name = gm.Name.TextValue;
        RunLog.Warning($"GM detected nearby: {name} (OnlineStatus {gm.OnlineStatus.RowId}). Firing the GM alert.");
        if (configuration.GmAlertStopRun)
        {
            RunGuarded(() =>
            {
                Plugin.Instance.Controller.Stop();
            }, "stopping the run");
        }
        if (configuration.GmAlertToast)
        {
            RunGuarded(() =>
            {
                Svc.Toasts.ShowNormal("GM " + name + " is nearby!");
            }, "toast");
        }
        if (configuration.GmAlertChat)
        {
            RunGuarded(() =>
            {
                Svc.Chat.PrintError(AttgConstants.LogPrefix + " GM " + name + " is nearby!");
            }, "chat print");
        }
        if (configuration.GmAlertSound)
        {
            PlayBeeps(configuration.GmAlertBeepCount, configuration.GmAlertBeepFrequencyHz, configuration.GmAlertBeepDurationMs);
        }
        var gmAlertCommands = configuration.GmAlertCommands;
        for (int i = 0; i < gmAlertCommands.Count; i++)
        {
            var command = gmAlertCommands[i];
            RunGuarded(() =>
            {
                Chat.ExecuteCommand(command);
            }, "command '" + command + "'");
        }
        if (configuration.GmAlertKillGame)
        {
            RunGuarded(() =>
            {
                Chat.ExecuteCommand(KillGameCommand);
            }, KillGameCommand);
        }
    }

    private static void RunGuarded(Action reaction, string what)
    {
        try
        {
            reaction();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "GM alert: " + what + " threw.");
        }
    }
}
