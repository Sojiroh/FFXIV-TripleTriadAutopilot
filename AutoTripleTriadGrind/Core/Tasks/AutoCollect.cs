using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Ipc;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Decks;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Tasks;

internal sealed class AutoCollect(AutoTriadSession session, TriadProgress progress) : AutoCommon
{
    private const int ReadyWaitMs = 30000;

    private readonly AutoTriadSession session = session;

    private readonly TriadProgress progress = progress;

    private readonly HashSet<int> excluded = new();

    protected override async Task Execute()
    {
        try
        {
            await Collect();
        }
        catch (Exception exception)
        {
            session.RecordFault(exception, CancelToken);
            throw;
        }
        finally
        {
            TriadDeckBuilder.Cancel();
            NavmeshIPC.Instance.Stop();
        }
    }

    private async Task Collect()
    {
        TriadData.EnsureLoaded();
        if (!TriadData.Loaded)
        {
            Svc.Chat.PrintError("[TTA] " + TriadData.FailureReason);
            return;
        }
        if (!await WaitUntilTimed(NpcInteraction.PlayerReady, ReadyWaitMs, "collect-ready"))
        {
            return;
        }
        progress.SetPhase(TriadPhase.Registering);
        await RegisterPendingCards(session);
        var configuration = Plugin.Instance.Configuration;
        while (!CancelToken.IsCancellationRequested)
        {
            progress.SetPhase(TriadPhase.Planning);
            TriadOwnership.Refresh(force: true);
            var plan = CollectPlanner.Build(configuration, excluded);
            var assignments = plan.Assignments;
            if (assignments.Length == 0)
            {
                Diag($"Collect: nothing left to play ({plan.WantedCards} picked cards missing, {plan.Unavailable.Length} unavailable).");
                session.CompletedByStopCondition = true;
                return;
            }
            var queue = new ushort[assignments.Length];
            for (var i = 0; i < assignments.Length; i++)
            {
                queue[i] = assignments[i].NpcIndex;
            }
            progress.SetQueue(queue);
            for (var index = 0; index < assignments.Length && !CancelToken.IsCancellationRequested; index++)
            {
                progress.SetQueueNext(index);
                var cards = assignments[index].Cards;
                var run = new TriadNpcRun(assignments[index].NpcIndex, _ => AllCollected(cards))
                {
                    MatchLimit = configuration.MaxMatchesPerNpc
                };
                Status = "Collecting from " + run.Name;
                var npcResult = await PlayNpc(run, session, progress);
                switch (npcResult.End)
                {
                    case NpcRunEnd.GoalMet:
                        session.RecordNpcCompleted();
                        break;
                    case NpcRunEnd.Skipped:
                        Diag($"Collect: skipping {run.Name} ({npcResult.Reason}).");
                        session.RecordSkip(run.NpcIndex, npcResult.Reason);
                        excluded.Add(run.NpcIndex);
                        break;
                    default:
                        return;
                }
            }
            progress.SetQueueNext(assignments.Length);
        }
    }

    private static bool AllCollected(ushort[] cards)
    {
        for (var i = 0; i < cards.Length; i++)
        {
            if (!CardRegistrar.IsCollected(cards[i]))
            {
                return false;
            }
        }
        return true;
    }
}
