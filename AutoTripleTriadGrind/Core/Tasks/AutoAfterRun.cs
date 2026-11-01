using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Ipc;
using ECommons.Automation;

namespace AutoTripleTriadGrind.Core.Tasks;

public sealed class AutoAfterRun(AfterRunAction action, string postRunCommands) : AutoCommon
{
    private const int ConfirmationWaitMs = 6000;

    private const int PreCommandSettleMs = 800;

    private const int LogoutAttempts = 2;

    private const string LogoutCommand = "/logout";

    private const string CloseGameCommand = "/xlkill";

    private readonly AfterRunAction action = action;

    private readonly string postRunCommands = postRunCommands;

    protected override async Task Execute()
    {
        try
        {
            await FinishRun();
        }
        finally
        {
            NavmeshIPC.Instance.Stop();
        }
    }

    private async Task FinishRun()
    {
        if (!await WaitUntilSafeToFinish("after-run-ready"))
        {
            if (!CancelToken.IsCancellationRequested)
            {
                Warn($"After-run chat commands and {action} skipped: no safe moment (combat, casting, or a zone change) within {20}s.");
            }
            return;
        }
        await SendPostRunCommands(postRunCommands);
        switch (action)
        {
            case AfterRunAction.Logout:
                await LogOut();
                break;
            case AfterRunAction.CloseGame:
                await CloseGame();
                break;
        }
    }

    private async Task LogOut()
    {
        Status = "Logging out";
        for (var attempt = 1; attempt <= LogoutAttempts; attempt++)
        {
            await DelayMs(PreCommandSettleMs);
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            Diag($"After-run: sending {LogoutCommand} (attempt {attempt}/{LogoutAttempts}).");
            Chat.ExecuteCommand(LogoutCommand);
            if (await WaitUntilTimed(DialogDriver.ConfirmationOpen, ConfirmationWaitMs, $"logout-confirmation#{attempt}") && DialogDriver.Confirm())
            {
                Diag("After-run: logout confirmation accepted.");
                return;
            }
        }
        if (!CancelToken.IsCancellationRequested)
        {
            Warn($"After-run: the logout confirmation was not accepted after {LogoutAttempts} tries; the character may still be logged in.");
        }
    }

    private async Task CloseGame()
    {
        Status = "Closing the game";
        await DelayMs(PreCommandSettleMs);
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        Diag("After-run: closing the game with /xlkill.");
        Chat.ExecuteCommand(CloseGameCommand);
    }
}
