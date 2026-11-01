using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core.Game.Ops;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Travel;
using AutoTripleTriadGrind.Core.Triad.Addons;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Decks;
using AutoTripleTriadGrind.Core.Triad.Logic;
using AutoTripleTriadGrind.Core.Triad.Logic.Agents;
using AutoTripleTriadGrind.Core.Triad.Match;
using Dalamud.Game.ClientState.Conditions;

namespace AutoTripleTriadGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int WindowTimeoutMs = 15000;

    private const int MatchTimeoutMs = 480000;

    private const int UnreadableBoardFallbackMs = 20000;

    private const int RetryFrames = 15;

    private const int DeckSelectAttempts = 12;

    private const int RegionalSlots = 2;

    private async Task<NpcRunResult?> PlayMatchSeries(Configuration configuration, TriadNpcRun run, AutoTriadSession session, TriadProgress progress)
    {
        while (!CancelToken.IsCancellationRequested)
        {
            var resumeAtResult = TriadAddons.IsVisible("TripleTriadResult");
            var matchWindowOpen = !resumeAtResult && TriadAddons.IsVisible("TripleTriad");
            if (!resumeAtResult && !matchWindowOpen)
            {
                if (!await AcceptChallenge(configuration, run, progress))
                {
                    await LeaveMatchWindows();
                    return null;
                }
                if (!await SelectDeck(configuration))
                {
                    Warn("Deck selection for " + run.Name + " did not go through.");
                    await LeaveMatchWindows();
                    return new NpcRunResult(NpcRunEnd.Skipped, SkipReason.InteractFailed);
                }
            }
            progress.SetPhase(TriadPhase.Playing);
            if (!resumeAtResult && !await PlayBoard(run))
            {
                await LeaveMatchWindows();
                return null;
            }
            var (matchOutcome, rewardItem) = await ReadResult();
            run.Matches++;
            run.LossStreak = ((matchOutcome == MatchOutcome.Lost) ? (run.LossStreak + 1) : 0);
            session.RecordMatch(matchOutcome);
            progress.RecordNpcMatch(matchOutcome);
            var rewardIsNewCard = TriadData.Set.CardIdByItemId.TryGetValue(rewardItem, out var rewardCardId) && !TriadOwnership.IsOwned(rewardCardId) && !unregistrableItems.Contains(rewardItem);
            Diag($"{run.Name}: match {run.Matches} {matchOutcome}, reward item {rewardItem}{(rewardIsNewCard ? (" (" + TriadData.Set.CardName(rewardCardId) + ", new)") : string.Empty)}.");
            if (rewardIsNewCard)
            {
                session.RecordCardObtained(rewardCardId);
            }
            if (rewardIsNewCard || run.GoalMet(run) || Stop(configuration, run).HasValue || run.MatchLimitReached)
            {
                await LeaveMatchWindows();
                return (run.MatchLimitReached && !run.GoalMet(run)) ? new NpcRunResult(NpcRunEnd.Skipped, SkipReason.MatchLimit) : null;
            }
            if (configuration.DelayBetweenMatchesMs > 0)
            {
                await DelayMs(configuration.DelayBetweenMatchesMs);
            }
            progress.SetPhase(TriadPhase.Rematch);
            if (!await Rematch())
            {
                await LeaveMatchWindows();
                return null;
            }
        }
        return new NpcRunResult(NpcRunEnd.StopRun, SkipReason.None);
    }

    private async Task<bool> AcceptChallenge(Configuration configuration, TriadNpcRun run, TriadProgress progress)
    {
        if (!await WaitUntilTimed(() => TriadAddons.IsReady("TripleTriadRequest") || TriadAddons.IsVisible("TripleTriadSelDeck"), WindowTimeoutMs, "triad-request"))
        {
            return false;
        }
        RememberRegionalRules(configuration, run);
        if (!await EnsureDeck(configuration, run, progress))
        {
            return false;
        }
        progress.SetPhase(TriadPhase.Challenging);
        var deadline = Environment.TickCount64 + WindowTimeoutMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (TriadAddons.IsVisible("TripleTriadSelDeck") || TriadAddons.IsVisible("TripleTriad"))
            {
                return true;
            }
            var step = TriadDialog.Advance();
            if (step is TriadDialog.Step.Nothing or TriadDialog.Step.UnknownMenu)
            {
                TriadMatchOps.ClickChallenge();
            }
            await NextFrame(RetryFrames);
        }
        return TriadAddons.IsVisible("TripleTriadSelDeck") || TriadAddons.IsVisible("TripleTriad");
    }

    private static void RememberRegionalRules(Configuration configuration, TriadNpcRun run)
    {
        Span<TriadRuleId> readRules = stackalloc TriadRuleId[RegionalSlots];
        if (!TriadMatchOps.TryReadRegionalRules(readRules) || readRules.SequenceEqual(run.Regional))
        {
            return;
        }
        readRules.CopyTo(run.Regional);
        configuration.RegionalRules[run.Npc.TriadRowId] = run.Regional.ToArray();
        configuration.SaveDebounced();
    }

    private async Task<bool> EnsureDeck(Configuration configuration, TriadNpcRun run, TriadProgress progress)
    {
        if (configuration.DeckSource != DeckSource.Optimized)
        {
            return ProfileDecks.HasCards(configuration.OwnDeckSlot) || WarnFalse($"Saved deck {configuration.OwnDeckSlot + 1} is empty.");
        }
        var ruleMask = TriadDeckBuilder.RuleMask(run.NpcIndex, run.Regional);
        var deck = run.Deck;
        if (deck != null && deck.RuleMask == ruleMask)
        {
            return WriteDeck(configuration, deck);
        }
        if (!TriadDeckBuilder.TryGetCached(configuration, run.NpcIndex, ruleMask, out var builtDeck))
        {
            progress.SetPhase(TriadPhase.Optimizing);
            TriadDeckBuilder.Start(configuration, run.NpcIndex, ruleMask);
            while (!CancelToken.IsCancellationRequested)
            {
                progress.SetOptimizerProgress(TriadDeckBuilder.Progress);
                if (TriadDeckBuilder.TryCollect(configuration) != null || !TriadDeckBuilder.IsBuilding)
                {
                    break;
                }
                await NextFrame(10);
            }
            if (!TriadDeckBuilder.TryGetCached(configuration, run.NpcIndex, ruleMask, out builtDeck))
            {
                return WarnFalse("No deck could be built for " + run.Name + ".");
            }
        }
        run.DeckReady = true;
        run.Deck = builtDeck;
        return WriteDeck(configuration, builtDeck);
    }

    private bool WriteDeck(Configuration configuration, CachedDeck deck)
    {
        if (!ProfileDecks.TryWrite(configuration.OptimizedDeckSlot, deck.Cards))
        {
            return WarnFalse("The optimized deck could not be written to the deck slot.");
        }
        return true;
    }

    private async Task<bool> SelectDeck(Configuration configuration)
    {
        var slot = ((configuration.DeckSource == DeckSource.Optimized) ? configuration.OptimizedDeckSlot : configuration.OwnDeckSlot);
        if (!await WaitUntilTimed(() => TriadAddons.IsReady("TripleTriadSelDeck") || TriadAddons.IsVisible("TripleTriad"), WindowTimeoutMs, "triad-deck-select"))
        {
            return false;
        }
        for (var attempt = 0; attempt < DeckSelectAttempts && !CancelToken.IsCancellationRequested; attempt++)
        {
            if (TriadMatchOps.DeckChosen())
            {
                return true;
            }
            TriadMatchOps.ChooseDeck(slot, attempt);
            for (var wait = 0; wait < 2 && !TriadMatchOps.DeckChosen(); wait++)
            {
                await NextFrame(RetryFrames);
            }
        }
        return TriadMatchOps.DeckChosen();
    }

    private async Task<bool> PlayBoard(TriadNpcRun run)
    {
        TriadRolloutAgent.MaxParallelism = AttgConstants.BackgroundWorkerThreads;
        var memory = run.Memory;
        var screen = run.Screen;
        Task<(bool Found, int Card, int Cell)>? solving = null;
        var needMove = true;
        var couldAct = false;
        var unreadableSince = 0L;
        var deadline = Environment.TickCount64 + MatchTimeoutMs;
        while (!CancelToken.IsCancellationRequested)
        {
            if (TriadAddons.IsVisible("TripleTriadResult"))
            {
                return true;
            }
            if (Environment.TickCount64 > deadline)
            {
                Warn($"The match against {run.Name} ran past {8} minutes.");
                return false;
            }
            if (!TriadMatchOps.TryReadBoard(screen, out var frame))
            {
                await NextFrame(2);
                continue;
            }
            if (frame.CanAct && !couldAct)
            {
                needMove = true;
            }
            couldAct = frame.CanAct;
            if (solving != null)
            {
                if (solving.IsCompleted)
                {
                    (bool, int, int) solved = (solving.IsCompletedSuccessfully ? solving.Result : default);
                    solving = null;
                    if (solved.Item1 && frame.CanAct)
                    {
                        TriadMatchOps.PlaceCard(solved.Item2, solved.Item3);
                        needMove = false;
                    }
                }
                await NextFrame();
                continue;
            }
            if (!frame.CanAct || !needMove)
            {
                unreadableSince = 0L;
                await NextFrame();
                continue;
            }
            if (frame.ReadFailed)
            {
                unreadableSince = ((unreadableSince == 0L) ? Environment.TickCount64 : unreadableSince);
                if (Environment.TickCount64 - unreadableSince > UnreadableBoardFallbackMs && TriadMatchOps.PlayAnyCard(screen))
                {
                    Warn("Part of the board could not be read, so a card was played without the solver.");
                    needMove = false;
                    unreadableSince = 0L;
                }
                await NextFrame(5);
                continue;
            }
            unreadableSince = 0L;
            memory.OnNewScan(screen, run.NpcDeck);
            var snapshot = new TriadGameState(memory.GameState);
            var solver = memory.Solver;
            solving = Task.Run(() =>
            {
                var found = solver.FindNextMove(snapshot, out var cardIndex, out var boardPosition, out var _);
                if (snapshot.ForcedCardIndex >= 0)
                {
                    cardIndex = snapshot.ForcedCardIndex;
                }
                return (found && cardIndex >= 0 && boardPosition >= 0, card: cardIndex, cell: boardPosition);
            });
            await NextFrame();
        }
        return false;
    }

    private async Task<(MatchOutcome Outcome, uint RewardItem)> ReadResult()
    {
        await WaitUntilTimed(TriadMatchOps.ResultReady, WindowTimeoutMs, "triad-result");
        var outcome = MatchOutcome.Unknown;
        var deadline = Environment.TickCount64 + 3000;
        while (outcome == MatchOutcome.Unknown && Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            outcome = TriadMatchOps.ReadOutcome();
            if (outcome == MatchOutcome.Unknown)
            {
                await NextFrame(5);
            }
        }
        await DelayMs(500);
        return (Outcome: outcome, RewardItem: AutoTripleTriadGrind.Core.Triad.Addons.TriadAgent.ReadRewardItemId());
    }

    private async Task<bool> Rematch()
    {
        for (var attempt = 0; attempt < 6 && !CancelToken.IsCancellationRequested; attempt++)
        {
            if (TriadAddons.IsVisible("TripleTriadRequest") || TriadAddons.IsVisible("TripleTriadSelDeck"))
            {
                return true;
            }
            TriadMatchOps.Rematch();
            await NextFrame(30);
        }
        return TriadAddons.IsVisible("TripleTriadRequest") || TriadAddons.IsVisible("TripleTriadSelDeck");
    }

    internal async Task LeaveMatchWindows()
    {
        for (var attempt = 0; attempt < 20 && !CancelToken.IsCancellationRequested; attempt++)
        {
            if (!TriadMatchOps.CloseOneWindow())
            {
                return;
            }
            await NextFrame(RetryFrames);
        }
    }

    private bool WarnFalse(string message)
    {
        Warn(message);
        return false;
    }

    private const float WalkArrivalSlackMeters = 2f;

    private const float FlightHoverSlackMeters = 6f;

    internal static bool WithinReach(Vector3 destination, float tolerance)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        var position = localPlayer.Position;
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return Vector3.Distance(position, destination) <= tolerance + WalkArrivalSlackMeters;
        }
        if (GroundDistance.Between(position, destination) <= tolerance + WalkArrivalSlackMeters)
        {
            return MathF.Abs(position.Y - destination.Y) <= tolerance + FlightHoverSlackMeters;
        }
        return false;
    }

    private const int DismountWatchdogMs = 30000;

    internal Task<bool> DismountViaOp(string label, int watchdogMs = DismountWatchdogMs, Func<bool>? abortIf = null)
    {
        return RunCancellable(new MoveOp(move => move.DismountNow()), watchdogMs, label, abortIf);
    }
}
