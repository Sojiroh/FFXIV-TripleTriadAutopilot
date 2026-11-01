using System;
namespace AutoTripleTriadGrind.Core.Triad.Logic.Agents;

public abstract class TriadAgent
{
    protected int SessionSeed;

    public virtual void Initialize(int sessionSeed)
    {
        SessionSeed = sessionSeed;
    }

    public virtual void OnSimulationStart()
    {
    }

    public abstract bool FindNextMove(TriadSolver solver, TriadGameState state, out int cardIndex, out int boardPosition, out TriadSolverResult result);

    protected static bool IsFinished(TriadGameState state, out TriadSolverResult result)
    {
        switch (state.Status)
        {
            case TriadGameStatus.BlueWins:
                result = new TriadSolverResult(1f, 0f, 1L);
                return true;
            case TriadGameStatus.BlueDraw:
                result = new TriadSolverResult(0f, 1f, 1L);
                return true;
            case TriadGameStatus.BlueLost:
                result = new TriadSolverResult(0f, 0f, 1L);
                return true;
            default:
                result = TriadSolverResult.Zero;
                return false;
        }
    }

    public static int PickBit(int mask, int step)
    {
        for (var i = 0; 1 << i <= mask && i < 31; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                step--;
                if (step < 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }
}

public sealed class TriadRandomAgent : TriadAgent
{
    private Random random;

    public TriadRandomAgent(int sessionSeed)
    {
        random = new Random(sessionSeed);
        SessionSeed = sessionSeed;
    }

    public override void Initialize(int sessionSeed)
    {
        SessionSeed = sessionSeed;
        random = new Random(sessionSeed);
    }

    public override bool FindNextMove(TriadSolver solver, TriadGameState state, out int cardIndex, out int boardPosition, out TriadSolverResult result)
    {
        cardIndex = -1;
        boardPosition = -1;
        result = TriadSolverResult.Zero;
        if (state.NumCardsPlaced < TriadGameState.BoardCells)
        {
            var boardIndex = random.Next(TriadGameState.BoardCells);
            for (var i = 0; i < TriadGameState.BoardCells; i++)
            {
                boardIndex = (boardIndex + 1) % TriadGameState.BoardCells;
                if (state.Board[boardIndex].IsEmpty)
                {
                    boardPosition = boardIndex;
                    break;
                }
            }
        }
        var availableCardMask = state.TurnDeck.AvailableCardMask;
        if (availableCardMask > 0)
        {
            var cardSlot = random.Next(TriadDeckInstance.MaxAvailableCards);
            for (var j = 0; j < TriadDeckInstance.MaxAvailableCards; j++)
            {
                cardSlot = (cardSlot + 1) % TriadDeckInstance.MaxAvailableCards;
                if ((availableCardMask & (1 << cardSlot)) != 0)
                {
                    cardIndex = cardSlot;
                    break;
                }
            }
        }
        return boardPosition >= 0 && cardIndex >= 0;
    }
}
