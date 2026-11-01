using System;
using System.Collections.Generic;
using System.Threading;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Triad.Match;

namespace AutoTripleTriadGrind.Core.Tasks;

public sealed class AutoTriadSession(TriadRunMode mode)
{
    private readonly List<ushort> cardsObtained = new();

    private readonly List<SkippedNpc> skippedNpcs = new();

    private long pausedMs;

    private long pauseStartedAtMs;

    private DateTime? endedAt;

    public bool CompletedByStopCondition;

    public bool Recorded;

    public bool AfterActionDispatched;

    public TriadRunMode Mode { get; } = mode;

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public int MatchesWon { get; private set; }

    public int MatchesLost { get; private set; }

    public int MatchesDrawn { get; private set; }

    public int NpcsCompleted { get; private set; }

    public int CardsRegistered { get; private set; }

    public IReadOnlyList<ushort> CardsObtained => cardsObtained;

    public IReadOnlyList<SkippedNpc> SkippedNpcs => skippedNpcs;

    public bool EndedWithFault { get; private set; }

    public int MatchesPlayed => MatchesWon + MatchesLost + MatchesDrawn;

    public bool DidNothing
    {
        get
        {
            if (MatchesPlayed == 0)
            {
                return CardsRegistered == 0;
            }
            return false;
        }
    }

    public TimeSpan Elapsed => (endedAt ?? DateTime.UtcNow) - StartedAt - TimeSpan.FromMilliseconds(PausedTotalMs);

    private long PausedTotalMs => pausedMs + ((pauseStartedAtMs == 0L) ? 0 : (Environment.TickCount64 - pauseStartedAtMs));

    public void RecordMatch(MatchOutcome outcome)
    {
        switch (outcome)
        {
            case MatchOutcome.Won:
                MatchesWon++;
                break;
            case MatchOutcome.Lost:
                MatchesLost++;
                break;
            case MatchOutcome.Drawn:
                MatchesDrawn++;
                break;
        }
    }

    public void RecordCardObtained(ushort cardId)
    {
        if (cardsObtained.Contains(cardId))
        {
            return;
        }
        cardsObtained.Add(cardId);
    }

    public void RecordCardRegistered()
    {
        CardsRegistered++;
    }

    public void RecordNpcCompleted()
    {
        NpcsCompleted++;
    }

    public void RecordSkip(ushort npcIndex, SkipReason reason)
    {
        skippedNpcs.Add(new SkippedNpc(npcIndex, reason));
    }

    public void RecordFault(Exception exception, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested || exception is OperationCanceledException)
        {
            return;
        }
        EndedWithFault = true;
        RunLog.Record(RunLogLevel.Error, exception, "The triad task ended with an unexpected error");
    }

    internal void ClearFault()
    {
        EndedWithFault = false;
    }

    public void BeginPause()
    {
        if (pauseStartedAtMs != 0L)
        {
            return;
        }
        pauseStartedAtMs = Environment.TickCount64;
    }

    public void EndPause()
    {
        if (pauseStartedAtMs == 0L)
        {
            return;
        }
        pausedMs += Environment.TickCount64 - pauseStartedAtMs;
        pauseStartedAtMs = 0L;
    }

    internal void End()
    {
        var ended = endedAt;
        if (ended.HasValue)
        {
            return;
        }
        EndPause();
        endedAt = DateTime.UtcNow;
    }
}

public readonly record struct SkippedNpc(ushort NpcIndex, SkipReason Reason);
