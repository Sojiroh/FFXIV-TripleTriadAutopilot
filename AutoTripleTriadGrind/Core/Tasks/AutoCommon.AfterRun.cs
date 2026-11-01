using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using clib.Services;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon : AutoTask
{
    protected const int AfterRunReadyWaitMs = 20000;

    private const int PostRunCommandSpacingMs = 500;

    protected Task<bool> WaitUntilSafeToFinish(string scope) => WaitUntilTimed(IsSafeToFinish, AfterRunReadyWaitMs, scope);

    protected async Task SendPostRunCommands(string commandBlock)
    {
        var lines = PostRunCommandLines.Split(commandBlock);
        if (lines.Length == 0)
        {
            return;
        }
        Status = "Sending your chat commands";
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            var line = lines[lineIndex];
            if (!PostRunCommandLines.IsCommand(line))
            {
                Warn("Post-run line skipped, commands must start with '/': " + line);
                continue;
            }
            SendPostRunCommand(line);
            await DelayMs(PostRunCommandSpacingMs);
        }
    }

    private void SendPostRunCommand(string command)
    {
        try
        {
            Diag("Post-run command: " + command);
            Chat.ExecuteCommand(command);
        }
        catch (Exception ex)
        {
            RunLog.Error("Post-run command '" + command + "' failed: " + ex.Message);
        }
    }

    private static bool IsSafeToFinish() => ECommons.DalamudServices.Svc.Objects.LocalPlayer != null && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting];
}
