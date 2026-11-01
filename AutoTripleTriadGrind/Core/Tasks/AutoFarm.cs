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

internal sealed class AutoFarm(AutoTriadSession session, TriadProgress progress) : AutoCommon
{
    private const int ReadyWaitMs = 30000;

    private readonly AutoTriadSession session = session;

    private readonly TriadProgress progress = progress;

    protected override async Task Execute()
    {
        try
        {
            await Farm();
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

    private async Task Farm()
    {
        TriadData.EnsureLoaded();
        if (!TriadData.Loaded)
        {
            Svc.Chat.PrintError("[TTA] " + TriadData.FailureReason);
            return;
        }
        if (!await WaitUntilTimed(NpcInteraction.PlayerReady, ReadyWaitMs, "farm-ready"))
        {
            return;
        }
        var configuration = Plugin.Instance.Configuration;
        var targets = EligibleTargets(configuration);
        if (targets.Length == 0)
        {
            Svc.Chat.PrintError("[TTA] None of the picked NPCs can be played right now.");
            return;
        }
        progress.SetQueue(targets);
        var limit = Math.Max(1, configuration.FarmMatchLimit);
        var endless = configuration.FarmStop == FarmStopKind.Never;
        for (var index = 0; index < targets.Length && !CancelToken.IsCancellationRequested; index++)
        {
            progress.SetQueueNext(index);
            var run = new TriadNpcRun(targets[index], run => !endless && run.Matches >= limit);
            Status = "Farming " + run.Name;
            var npcResult = await PlayNpc(run, session, progress);
            switch (npcResult.End)
            {
                case NpcRunEnd.GoalMet:
                    session.RecordNpcCompleted();
                    break;
                case NpcRunEnd.Skipped:
                    Diag($"Farm: skipping {run.Name} ({npcResult.Reason}).");
                    session.RecordSkip(run.NpcIndex, npcResult.Reason);
                    break;
                default:
                    return;
            }
        }
        progress.SetQueueNext(targets.Length);
        session.CompletedByStopCondition = !CancelToken.IsCancellationRequested;
    }

    private static ushort[] EligibleTargets(Configuration configuration)
    {
        var set = TriadData.Set;
        var eligible = new List<ushort>(configuration.FarmNpcs.Count);
        for (var i = 0; i < configuration.FarmNpcs.Count; i++)
        {
            if (set.NpcIndexByTriadRowId.TryGetValue(configuration.FarmNpcs[i], out var npcIndex) && NpcEligibility.Check(npcIndex, configuration) == SkipReason.None)
            {
                eligible.Add(npcIndex);
            }
        }
        return eligible.ToArray();
    }
}
