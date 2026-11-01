using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTripleTriadGrind.Core.Triad.Logic.Agents;

public abstract class TriadGraphAgent : TriadAgent
{
    private const int MaxSearchDepth = 20;

    private Random? failsafeRandom;

    public override bool FindNextMove(TriadSolver solver, TriadGameState state, out int cardIndex, out int boardPosition, out TriadSolverResult result)
    {
        cardIndex = -1;
        boardPosition = -1;
        if (!TriadAgent.IsFinished(state, out result))
        {
            SearchActionSpace(solver, state, 0, out cardIndex, out boardPosition, out result);
        }
        return cardIndex >= 0 && boardPosition >= 0;
    }

    protected virtual TriadSolverResult SearchActionSpace(TriadSolver solver, TriadGameState state, int searchLevel, out int bestCardIndex, out int bestBoardPosition, out TriadSolverResult bestResult)
    {
        bestCardIndex = -1;
        bestBoardPosition = -1;
        bestResult = TriadSolverResult.Zero;
        if (searchLevel > MaxSearchDepth)
        {
            return bestResult;
        }
        var wins = 0f;
        var draws = 0f;
        var games = 0L;
        solver.FindAvailableActions(state, out var boardMask, out var boardCount, out var cardsMask, out var cardsCount);
        if (cardsCount > 0 && boardCount > 0)
        {
            var turnOwner = state.TurnOwner;
            var foundMove = false;
            for (var i = 0; i < TriadDeckInstance.MaxAvailableCards; i++)
            {
                if ((cardsMask & (1 << i)) == 0)
                {
                    continue;
                }
                for (var j = 0; j < TriadGameState.BoardCells; j++)
                {
                    if ((boardMask & (1 << j)) == 0)
                    {
                        continue;
                    }
                    var nextState = new TriadGameState(state);
                    if (solver.Simulation.PlaceCard(nextState, i, nextState.TurnDeck, turnOwner, j))
                    {
                        if (!TriadAgent.IsFinished(nextState, out var result))
                        {
                            nextState.ForcedCardIndex = -1;
                            result = SearchActionSpace(solver, nextState, searchLevel + 1, out var _, out var _, out var _);
                        }
                        if (result.IsBetterThan(in bestResult) || !foundMove)
                        {
                            bestResult = result;
                            bestCardIndex = i;
                            bestBoardPosition = j;
                        }
                        wins += result.Wins;
                        draws += result.Draws;
                        games += result.Games;
                        foundMove = true;
                    }
                }
            }
            if (!foundMove)
            {
                if (failsafeRandom == null)
                {
                    failsafeRandom = new Random(SessionSeed);
                }
                bestCardIndex = TriadAgent.PickBit(cardsMask, failsafeRandom.Next(cardsCount));
                bestBoardPosition = TriadAgent.PickBit(boardMask, failsafeRandom.Next(boardCount));
            }
        }
        if (searchLevel % 2 != 0)
        {
            return new TriadSolverResult(wins, draws, games);
        }
        return bestResult;
    }
}

public class TriadRolloutAgent : TriadGraphAgent
{
    private const int RolloutBatchSize = 64;

    public static int RolloutCount { get; set; } = 2000;

    public static int MaxParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount);

    protected override TriadSolverResult SearchActionSpace(TriadSolver solver, TriadGameState state, int searchLevel, out int bestCardIndex, out int bestBoardPosition, out TriadSolverResult bestResult)
    {
        if (!CanRunRandomExploration(state, searchLevel))
        {
            return base.SearchActionSpace(solver, state, searchLevel, out bestCardIndex, out bestBoardPosition, out bestResult);
        }
        bestCardIndex = -1;
        bestBoardPosition = -1;
        bestResult = FindWinningProbability(solver, state);
        return bestResult;
    }

    protected virtual bool CanRunRandomExploration(TriadGameState state, int searchLevel)
    {
        return searchLevel > 0;
    }

    private TriadSolverResult FindWinningProbability(TriadSolver solver, TriadGameState state)
    {
        var rolloutCount = RolloutCount;
        var wins = 0;
        var draws = 0;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism
        };
        using var workerSolvers = new ThreadLocal<TriadSolver>(solver.CreateWorkerCopy);
        using var workerAgents = new ThreadLocal<TriadRandomAgent>(() => new TriadRandomAgent(SessionSeed + Environment.CurrentManagedThreadId));
        for (var batchStart = 0; batchStart < rolloutCount; batchStart += RolloutBatchSize)
        {
            var batchEnd = Math.Min(batchStart + RolloutBatchSize, rolloutCount);
            Parallel.For(batchStart, batchEnd, parallelOptions, (int rolloutIndex) =>
            {
                var rolloutState = new TriadGameState(state);
                var agent = workerAgents.Value!;
                agent.Initialize(SessionSeed + rolloutIndex);
                workerSolvers.Value!.RunSimulation(rolloutState, agent, agent);
                if (rolloutState.Status == TriadGameStatus.BlueWins)
                {
                    Interlocked.Increment(ref wins);
                }
                else if (rolloutState.Status == TriadGameStatus.BlueDraw)
                {
                    Interlocked.Increment(ref draws);
                }
            });
        }
        return new TriadSolverResult((float)wins / (float)rolloutCount, (float)draws / (float)rolloutCount, 1L);
    }
}
