using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Triad.Addons;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Decks;
using AutoTripleTriadGrind.Core.Triad.Logic;
using Dalamud.Game.ClientState.Objects.Types;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const float InteractRangeMeters = 6f;

    private const float TalkApproachToleranceMeters = 1.5f;

    private const int TalkApproachWatchdogMs = 10000;

    private const int MaxInteractFailures = 6;

    private const int ChallengeOpenTimeoutMs = 15000;

    private const int DialogSettleTimeoutMs = 8000;

    private const int DialogQuietMs = 2000;

    private const int MaxRefusedChallenges = 3;

    private const int MaxFailedSeries = 3;

    private readonly HashSet<uint> unregistrableItems = new();

    internal async Task<NpcRunResult> PlayNpc(TriadNpcRun run, AutoTriadSession session, TriadProgress progress)
    {
        var configuration = Plugin.Instance.Configuration;
        progress.BeginNpc(run.NpcIndex);
        LoadRegionalRules(configuration, run);
        PrepareDeck(configuration, run);
        progress.SetPhase(TriadPhase.Travelling);
        var npc = run.Npc;
        if (!await ReachNpc(run))
        {
            return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.Unreachable);
        }
        while (!CancelToken.IsCancellationRequested)
        {
            progress.SetPhase(TriadPhase.Registering);
            await RegisterPendingCards(session);
            TriadOwnership.Refresh(force: true);
            if (run.GoalMet(run))
            {
                return new NpcRunResult(NpcRunEnd.GoalMet, SkipReason.None);
            }
            if (Stop(configuration, run) is { } skipped)
            {
                return new NpcRunResult(NpcRunEnd.Skipped, skipped);
            }
            if (CardRegistrar.FreeBagSlots() < configuration.MinFreeBagSlots)
            {
                ECommons.DalamudServices.Svc.Chat.PrintError("[TTA] Your bags are nearly full, so the run stops. Free a few slots and start again.");
                return new NpcRunResult(NpcRunEnd.StopRun, SkipReason.InventoryFull);
            }
            if (CardRegistrar.Mgp() < npc.Fee)
            {
                ECommons.DalamudServices.Svc.Chat.PrintError($"{AttgConstants.LogPrefix} You need {npc.Fee} MGP to challenge {run.Name}, so the run stops.");
                return new NpcRunResult(NpcRunEnd.StopRun, SkipReason.NotEnoughMgp);
            }
            progress.SetPhase(TriadPhase.Challenging);
            var attempt = await OpenChallenge(run);
            if (attempt == ChallengeAttempt.Declined)
            {
                await SettleDialog();
                return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.Locked);
            }
            if (attempt == ChallengeAttempt.QuestOffered)
            {
                await SettleDialog();
                return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.QuestOffered);
            }
            if (attempt == ChallengeAttempt.Failed)
            {
                await SettleDialog();
                run.InteractFailures++;
                if (run.InteractFailures >= MaxInteractFailures)
                {
                    return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.InteractFailed);
                }
                continue;
            }
            run.InteractFailures = 0;
            run.RefusedChallenges = 0;
            var matchesBefore = run.Matches;
            var leave = await PlayMatchSeries(configuration, run, session, progress);
            await SettleDialog();
            if (leave is { } result)
            {
                return result;
            }
            run.FailedSeries = ((run.Matches == matchesBefore) ? (run.FailedSeries + 1) : 0);
            if (run.FailedSeries >= MaxFailedSeries)
            {
                return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.InteractFailed);
            }
        }
        return new NpcRunResult(NpcRunEnd.StopRun, SkipReason.None);
    }

    private static SkipReason? Stop(Configuration configuration, TriadNpcRun run)
    {
        if (configuration.LossStreakSkip > 0 && run.LossStreak >= configuration.LossStreakSkip)
        {
            return SkipReason.LossStreak;
        }
        return null;
    }

    private static void LoadRegionalRules(Configuration configuration, TriadNpcRun run)
    {
        if (!configuration.RegionalRules.TryGetValue(run.Npc.TriadRowId, out var regionalRules))
        {
            return;
        }
        for (var i = 0; i < run.Regional.Length && i < regionalRules.Length; i++)
        {
            run.Regional[i] = regionalRules[i];
        }
    }

    private static void PrepareDeck(Configuration configuration, TriadNpcRun run)
    {
        if (configuration.DeckSource != DeckSource.Optimized)
        {
            run.DeckReady = true;
            return;
        }
        var ruleMask = TriadDeckBuilder.RuleMask(run.NpcIndex, run.Regional);
        if (TriadDeckBuilder.TryGetCached(configuration, run.NpcIndex, ruleMask, out var _))
        {
            run.DeckReady = true;
            return;
        }
        run.DeckReady = false;
        TriadDeckBuilder.Start(configuration, run.NpcIndex, ruleMask);
    }

    private async Task<bool> ReachNpc(TriadNpcRun run)
    {
        var npc = run.Npc;
        var npcObject = NpcInteraction.FindNearest(npc.ENpcBaseId);
        if (npcObject != null && ECommons.DalamudServices.Svc.ClientState.TerritoryType == npc.TerritoryId && WithinReach(npcObject.Position, InteractRangeMeters))
        {
            return true;
        }
        if (!await TravelTo(npc.TerritoryId, npc.Position, InteractRangeMeters))
        {
            Warn($"Could not reach {run.Name} in territory {npc.TerritoryId}.");
            return false;
        }
        var closestNpc = NpcInteraction.FindNearest(npc.ENpcBaseId);
        if (closestNpc != null && !WithinReach(closestNpc.Position, InteractRangeMeters))
        {
            await TravelTo(npc.TerritoryId, closestNpc.Position, InteractRangeMeters);
        }
        await SafeDismount("triad-npc");
        return NpcInteraction.FindNearest(npc.ENpcBaseId) is not null;
    }

    private async Task<ChallengeAttempt> OpenChallenge(TriadNpcRun run)
    {
        if (TriadAddons.AnyMatchWindowVisible())
        {
            return ChallengeAttempt.Opened;
        }
        var deadline = Environment.TickCount64 + ChallengeOpenTimeoutMs;
        var lastInteract = 0L;
        var approached = false;
        var awaitingAnswer = false;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (TriadAddons.IsVisible("TripleTriadRequest") || TriadAddons.IsVisible("TripleTriadSelDeck"))
            {
                return ChallengeAttempt.Opened;
            }
            var step = TriadDialog.Advance();
            if (step == TriadDialog.Step.QuestOffered)
            {
                Warn(run.Name + " offered a quest instead of the challenge; they will not play until it is accepted.");
                return ChallengeAttempt.QuestOffered;
            }
            if (step == TriadDialog.Step.UnknownMenu)
            {
                Warn(run.Name + " opened a menu without a Triple Triad entry.");
                return ChallengeAttempt.Failed;
            }
            if (step == TriadDialog.Step.ChoseChallenge)
            {
                awaitingAnswer = true;
            }
            if (((step == TriadDialog.Step.Nothing) & awaitingAnswer) && NpcInteraction.PlayerReady())
            {
                awaitingAnswer = false;
                if (++run.RefusedChallenges >= MaxRefusedChallenges)
                {
                    Warn($"{run.Name} turned the challenge down {run.RefusedChallenges} times; an unlock requirement is probably not met.");
                    return ChallengeAttempt.Declined;
                }
            }
            if (step == TriadDialog.Step.Nothing && NpcInteraction.PlayerReady() && Environment.TickCount64 - lastInteract > 1500 && NpcInteraction.FindNearest(run.Npc.ENpcBaseId) is { } target)
            {
                var justApproached = !approached;
                if (justApproached)
                {
                    justApproached = await ApproachToTalk(run, target);
                }
                if (justApproached)
                {
                    approached = true;
                    deadline = Environment.TickCount64 + ChallengeOpenTimeoutMs;
                    continue;
                }
                NpcInteraction.Target(target);
                NpcInteraction.Interact(target);
                lastInteract = Environment.TickCount64;
            }
            await NextFrame(5);
        }
        Warn("The challenge window for " + run.Name + " did not open.");
        return ChallengeAttempt.Failed;
    }

    private async Task<bool> ApproachToTalk(TriadNpcRun run, IGameObject target)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null || NpcInteraction.CanTalkTo(target))
        {
            return false;
        }
        var npcPosition = target.Position;
        var groundRoute = await ProbeGroundRoute(localPlayer.Position, npcPosition);
        var standPoint = ((groundRoute.Kind == GroundRouteKind.Unknown) ? npcPosition : groundRoute.End);
        Diag($"Walking up to {run.Name}, {DistanceTo(npcPosition):F1}m away, toward {FormatPosition(standPoint)} ({groundRoute.Kind} ground route).");
        var approach = new MoveOp(move => move.MoveInZone(standPoint, walkMovement.WithTolerance(TalkApproachToleranceMeters), () => NpcInteraction.CanTalkTo(target)));
        await RunCancellable(approach, TalkApproachWatchdogMs, "triad-approach", StuckDetector.MoveStallAbort("triad-approach"));
        var fault = approach.Fault;
        if (fault != null)
        {
            Diag("The walk up to " + run.Name + " faulted: " + fault.Message);
        }
        return true;
    }

    private async Task SettleDialog()
    {
        var deadline = Environment.TickCount64 + DialogSettleTimeoutMs;
        var quietSince = Environment.TickCount64;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            var tickCount = Environment.TickCount64;
            if (TriadAddons.AnyMatchWindowVisible())
            {
                TriadMatchOps.CloseOneWindow();
                quietSince = tickCount;
            }
            else
            {
                var step = TriadDialog.Advance(towardChallenge: false);
                if (step is TriadDialog.Step.Handled or TriadDialog.Step.QuestOffered)
                {
                    quietSince = tickCount;
                }
                else if (tickCount - quietSince >= DialogQuietMs)
                {
                    return;
                }
            }
            await NextFrame(5);
        }
    }

    private protected async Task RegisterPendingCards(AutoTriadSession session)
    {
        for (var attempt = 0; attempt < 20 && !CancelToken.IsCancellationRequested; attempt++)
        {
            var itemId = CardRegistrar.FindUnregisteredCardItem(unregistrableItems);
            if (itemId == 0 || !TriadData.Set.CardIdByItemId.TryGetValue(itemId, out var cardId))
            {
                return;
            }
            if (!await WaitUntilTimed(NpcInteraction.PlayerReady, 10000, "register-ready"))
            {
                return;
            }
            var cardName = TriadData.Set.CardName(cardId);
            Diag($"Registering {cardName} (item {itemId}).");
            CardRegistrar.Use(itemId);
            if (!await WaitUntilTimed(() =>
            {
                if (NpcInteraction.SelectYesnoOpen() && NpcInteraction.SelectYesnoText().Contains(cardName, StringComparison.OrdinalIgnoreCase))
                {
                    DialogDriver.Confirm();
                }
                TriadOwnership.Refresh(force: true);
                return TriadOwnership.IsOwned(cardId);
            }, 8000, "register-card"))
            {
                Warn(cardName + " did not register; leaving it in your bags.");
                unregistrableItems.Add(itemId);
                continue;
            }
            session.RecordCardRegistered();
            session.RecordCardObtained(cardId);
            await DelayMs(600);
        }
    }
}


internal enum NpcRunEnd : byte
{
    GoalMet,
    Skipped,
    StopRun
}

internal readonly record struct NpcRunResult(NpcRunEnd End, SkipReason Reason);

internal enum ChallengeAttempt : byte
{
    Opened,
    Failed,
    Declined,
    QuestOffered
}

internal sealed class TriadNpcRun(ushort npcIndex, Func<TriadNpcRun, bool> goalMet)
{
    public readonly ushort NpcIndex = npcIndex;

    public readonly Func<TriadNpcRun, bool> GoalMet = goalMet;

    public readonly TriadDeck NpcDeck = TriadDeckBuilder.NpcDeck(npcIndex);

    public readonly TriadScreenMemory Memory = new();

    public readonly TriadScreenState Screen = new();

    public readonly TriadRuleId[] Regional = new TriadRuleId[2];

    public int Matches;

    public int LossStreak;

    public int InteractFailures;

    public int RefusedChallenges;

    public int FailedSeries;

    public bool DeckReady;

    public CachedDeck? Deck;

    public int MatchLimit;

    public bool MatchLimitReached
    {
        get
        {
            if (MatchLimit > 0)
            {
                return Matches >= MatchLimit;
            }
            return false;
        }
    }

    public TriadNpc Npc => TriadData.Set.Npcs[NpcIndex];

    public string Name => TriadData.Set.NpcNames[NpcIndex];
}
