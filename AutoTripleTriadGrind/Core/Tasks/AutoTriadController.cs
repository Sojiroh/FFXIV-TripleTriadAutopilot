using System;
using System.Linq;
using AutoTripleTriadGrind.Core.External;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Stats;

namespace AutoTripleTriadGrind.Core.Tasks;

internal sealed class AutoTriadController
{
    private readonly TriadProgress progress = new();

    private AutoTriadSession? session;

    private AutoCommon? currentTask;

    private const int MaxFaultResumes = 3;

    private const int MaxFaultResumesPerRun = 6;

    private const long FaultResumeWindowMs = 300000L;

    private int faultResumeCount;

    private int runFaultResumeCount;

    private long faultWindowStartedAtMs;

    public bool Running
    {
        get
        {
            if (!clib.Services.Svc.Automation.Running)
            {
                return Paused;
            }
            return true;
        }
    }

    public string Status => PauseReason switch
    {
        PauseReason.InContent => "Paused while you are in content",
        PauseReason.Manual => "Paused",
        _ => clib.Services.Svc.Automation.CurrentTask?.Status ?? "Idle",
    };

    public TriadPhase Phase => progress.Phase;

    public TriadProgress Progress => progress;

    public AutoTriadSession? SessionSnapshot => session;

    public TriadRunMode Mode => session?.Mode ?? Plugin.Instance.Configuration.RunMode;

    public PauseReason PauseReason { get; private set; }

    public bool Paused => PauseReason != PauseReason.None;

    public bool CanPause
    {
        get
        {
            var canPause = !(session is null);
            if (canPause)
            {
                var phase = Phase;
                var inactive = phase is TriadPhase.Idle or TriadPhase.Finishing or TriadPhase.Paused;
                canPause = !inactive;
            }
            return canPause;
        }
    }

    private static void Diag(string message)
    {
        RunLog.Info(message);
    }

    public void Start(TriadRunMode mode)
    {
        if (!RequiredPluginsReady())
        {
            return;
        }
        PauseReason = PauseReason.None;
        ResetFaultBudget();
        session = new AutoTriadSession(mode);
        Diag($"Run starting: {mode}.");
        StartRun(session);
    }

    public void Stop()
    {
        var stoppedSession = session;
        var paused = Paused;
        currentTask = null;
        PauseReason = PauseReason.None;
        clib.Services.Svc.Automation.Stop();
        if (stoppedSession != null)
        {
            ReleaseHelpers();
        }
        FinalizeRun(stoppedSession);
        ClearRun();
        if (stoppedSession != null)
        {
            Diag("Stop requested" + (paused ? " while paused" : string.Empty) + "; session cleared.");
        }
    }

    private static bool RequiredPluginsReady()
    {
        if (ExternalPlugins.AllRequiredInstalled())
        {
            return true;
        }
        var missingNames = ExternalPlugins.MissingRequiredNames();
        Diag("Start aborted: required plugins missing (" + missingNames + ").");
        ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] Cannot start: install all required plugins first (" + missingNames + ").");
        return false;
    }

    private void StartRun(AutoTriadSession owningSession)
    {
        progress.Reset();
        progress.SetPhase(TriadPhase.Planning);
        RunTask(CreateRunTask(owningSession), () =>
        {
            OnRunEnded(owningSession);
        });
    }

    private AutoCommon CreateRunTask(AutoTriadSession owningSession)
    {
        return owningSession.Mode switch
        {
            TriadRunMode.Farm => new AutoFarm(owningSession, progress),
            _ => new AutoCollect(owningSession, progress),
        };
    }

    private void RunTask(AutoCommon task, Action onCompleted)
    {
        currentTask = task;
        clib.Services.Svc.Automation.Start(task, () =>
        {
            ECommons.DalamudServices.Svc.Framework.RunOnFrameworkThread(() =>
            {
                HandOff(task, onCompleted);
            });
        });
    }

    private void HandOff(AutoCommon task, Action onCompleted)
    {
        if (currentTask != task)
        {
            Diag(task.GetType().Name + " finished but is no longer the current task (stopped, paused, or superseded); skipping hand-off.");
            return;
        }
        currentTask = null;
        onCompleted();
    }

    private void ClearRun()
    {
        session = null;
        progress.Reset();
    }

    private static void ReleaseHelpers()
    {
        NavmeshIPC.Instance.Stop();
    }

    public void Pause(PauseReason reason)
    {
        if (reason == PauseReason.None)
        {
            return;
        }
        if (Paused)
        {
            if (reason == PauseReason.Manual && PauseReason == PauseReason.InContent)
            {
                PauseReason = PauseReason.Manual;
                Diag("Auto-pause promoted to a manual pause; leaving content will no longer resume.");
            }
            return;
        }
        if (!CanPause)
        {
            if (reason == PauseReason.Manual)
            {
                ECommons.DalamudServices.Svc.Chat.Print("[TTA] Nothing to pause.");
            }
            return;
        }
        var pausedSession = session!;
        PauseReason = reason;
        progress.SetPhase(TriadPhase.Paused);
        pausedSession.BeginPause();
        currentTask = null;
        clib.Services.Svc.Automation.Stop();
        ReleaseHelpers();
        Diag($"Run paused ({reason}); session kept at {pausedSession.MatchesPlayed} matches, {pausedSession.CardsObtained.Count} cards.");
        ECommons.DalamudServices.Svc.Chat.Print((reason == PauseReason.InContent) ? "[TTA] Paused: you are in instanced content. The run resumes once you are back outside." : "[TTA] Paused. Your run and session stats are kept until you resume or stop.");
    }

    public void Resume()
    {
        if (!Paused)
        {
            return;
        }
        var resumedSession = session;
        if (resumedSession == null)
        {
            Diag("Resume requested with no session or nothing to resume; stopping instead.");
            Stop();
            return;
        }
        PauseReason = PauseReason.None;
        resumedSession.EndPause();
        Diag("Resuming the run.");
        ECommons.DalamudServices.Svc.Chat.Print("[TTA] Resuming the run.");
        StartRun(resumedSession);
    }

    public void TogglePause()
    {
        if (Paused)
        {
            Resume();
        }
        else
        {
            Pause(PauseReason.Manual);
        }
    }

    private void OnRunEnded(AutoTriadSession owningSession)
    {
        var faulted = session == owningSession && owningSession.EndedWithFault;
        if (faulted && !owningSession.CompletedByStopCondition && TryAutoResumeAfterFault(owningSession))
        {
            return;
        }
        if (faulted)
        {
            ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] The run stopped on an unexpected error. The log has the details.");
        }
        EndRun(owningSession);
    }

    private void EndRun(AutoTriadSession owningSession)
    {
        FinalizeRun(owningSession);
        if (session != owningSession)
        {
            Diag("A run that is no longer live ended; it was recorded and the current run is left alone.");
        }
        else if (!TryRunAfterAction(owningSession))
        {
            ClearRun();
        }
    }

    private bool TryRunAfterAction(AutoTriadSession ending)
    {
        if (ending.AfterActionDispatched)
        {
            return false;
        }
        if (!ending.CompletedByStopCondition || ending.EndedWithFault)
        {
            Diag($"Run ended without meeting its stop condition (fault {ending.EndedWithFault}); no after-run action.");
            return false;
        }
        ending.AfterActionDispatched = true;
        var configuration = Plugin.Instance.Configuration;
        var action = configuration.AfterRun;
        var postRunCommands = configuration.PostRunCommands;
        if (action == AfterRunAction.StayLoggedIn && string.IsNullOrWhiteSpace(postRunCommands))
        {
            Diag("Run completed by its stop condition; the after-run action is StayLoggedIn and no chat commands are set, nothing to do.");
            return false;
        }
        if (ending.DidNothing)
        {
            Diag($"Run ended by its stop condition without doing any work; skipping chat commands and after-run action {action}.");
            return false;
        }
        Diag($"Run completed by its stop condition; starting chat commands and after-run action {action}.");
        progress.SetPhase(TriadPhase.Finishing);
        AutoCommon task = ((action == AfterRunAction.ReturnToInn) ? new AutoReturnToInn(postRunCommands) : new AutoAfterRun(action, postRunCommands));
        RunTask(task, () =>
        {
            Diag($"After-run action {action} finished.");
            ClearRun();
        });
        return true;
    }

    private void FinalizeRun(AutoTriadSession? ending)
    {
        if (ending == null || ending.Recorded)
        {
            return;
        }
        ending.Recorded = true;
        ending.End();
        try
        {
            if (ending.DidNothing)
            {
                Diag("Run did no work; nothing recorded to history.");
                return;
            }
            var runRecord = new RunRecord
            {
                Mode = ending.Mode,
                StartedAtUtc = ending.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = ending.Elapsed.TotalSeconds,
                MatchesWon = ending.MatchesWon,
                MatchesLost = ending.MatchesLost,
                MatchesDrawn = ending.MatchesDrawn,
                NpcsCompleted = ending.NpcsCompleted,
                NpcsSkipped = ending.SkippedNpcs.Count,
                CardsObtained = ending.CardsObtained.ToList()
            };
            Plugin.Instance.History.Append(runRecord);
            Diag($"Run recorded to history ({runRecord.Mode}): {runRecord.MatchesWon}/{runRecord.MatchesLost}/{runRecord.MatchesDrawn} W/L/D, {runRecord.CardsObtained.Count} cards, {runRecord.NpcsCompleted} NPCs over {runRecord.Duration}.");
        }
        catch (Exception ex)
        {
            Diag("FinalizeRun failed to record history: " + ex.Message);
        }
    }

    private void ResetFaultBudget()
    {
        faultResumeCount = 0;
        runFaultResumeCount = 0;
        faultWindowStartedAtMs = 0L;
    }

    private bool TryAutoResumeAfterFault(AutoTriadSession owningSession)
    {
        if (!Plugin.Instance.Configuration.AutoResumeOnFault)
        {
            Diag("Run task faulted and auto-resume on fault is off; the run ends.");
            return false;
        }
        if (runFaultResumeCount >= MaxFaultResumesPerRun)
        {
            Diag($"Run task faulted after {runFaultResumeCount} restarts in this run; not resuming. The run ends.");
            return false;
        }
        var now = Environment.TickCount64;
        if (now - faultWindowStartedAtMs > FaultResumeWindowMs)
        {
            faultResumeCount = 0;
            faultWindowStartedAtMs = now;
        }
        if (faultResumeCount >= MaxFaultResumes)
        {
            Diag($"Run task faulted {faultResumeCount} times within {5L} minutes; not resuming. The run ends.");
            return false;
        }
        faultResumeCount++;
        runFaultResumeCount++;
        owningSession.ClearFault();
        Diag($"Run task ended on an unexpected fault; auto-resuming (resume {faultResumeCount}/{MaxFaultResumes} in this {5L} minute window, {runFaultResumeCount}/{MaxFaultResumesPerRun} in this run).");
        ECommons.DalamudServices.Svc.Chat.Print($"{AttgConstants.LogPrefix} The run stopped on an unexpected error; restarting it ({faultResumeCount}/{MaxFaultResumes}).");
        StartRun(owningSession);
        return true;
    }
}

internal enum PauseReason
{
    None,
    Manual,
    InContent
}
