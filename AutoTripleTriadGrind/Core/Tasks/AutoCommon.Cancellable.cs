using System;
using System.Threading.Tasks;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    protected const int SubTaskUnwindGraceMs = 5000;

    private const int CancellablePollFrames = 4;

    internal async Task<bool> RunCancellable(MoveOp operation, int timeoutMs, string label, Func<bool>? abortIf = null)
    {
        var completion = new TaskCompletionSource();
        operation.Run(() =>
        {
            completion.TrySetResult();
        });
        var work = completion.Task;
        using var registration = CancelToken.Register(() =>
        {
            TryCancel(operation, label);
        });
        var deadline = Environment.TickCount64 + timeoutMs;
        var aborted = false;
        var abortThrew = false;
        while (!work.IsCompleted && Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                break;
            }
            if (abortIf != null && ShouldAbort(abortIf, label, ref abortThrew))
            {
                aborted = true;
                break;
            }
            await NextFrame(CancellablePollFrames);
        }
        if (work.IsCompleted)
        {
            return true;
        }
        if (CancelToken.IsCancellationRequested)
        {
            TryCancel(operation, label);
            return false;
        }
        Diag(aborted ? ("RunCancellable '" + label + "' aborting sub-task (abort condition met)") : $"WATCHDOG: '{label}' exceeded {timeoutMs / 1000}s; cancelling sub-task");
        TryCancel(operation, label);
        var grace = Environment.TickCount64 + SubTaskUnwindGraceMs;
        while (!work.IsCompleted && Environment.TickCount64 < grace)
        {
            await NextFrame(CancellablePollFrames);
        }
        if (!work.IsCompleted)
        {
            Diag($"WATCHDOG: '{label}' did not unwind {SubTaskUnwindGraceMs / 1000}s after Cancel (unexpected)");
        }
        return false;
    }

    private bool ShouldAbort(Func<bool> abortIf, string label, ref bool abortThrew)
    {
        try
        {
            return abortIf();
        }
        catch (Exception ex)
        {
            if (!abortThrew)
            {
                Warn("RunCancellable '" + label + "' abortIf threw (abort disabled, watchdog still active): " + ex.Message);
                abortThrew = true;
            }
            return false;
        }
    }

    private void TryCancel(MoveOp operation, string label)
    {
        try
        {
            operation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            Warn("RunCancellable '" + label + "' Cancel threw: " + ex.Message);
        }
    }
}
