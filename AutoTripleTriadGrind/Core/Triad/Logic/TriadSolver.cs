using System.Collections.Generic;
using System.Numerics;
using AutoTripleTriadGrind.Core.Triad.Logic.Agents;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadSolver
{
    public readonly TriadSimulation Simulation = new();

    public TriadAgent Agent { get; }

    public TriadSolver(TriadAgent agent)
    {
        Agent = agent;
        Agent.Initialize(0);
    }

    public static TriadSolver CreateLive()
    {
        return new TriadSolver(new TriadLiveAgent());
    }

    public TriadSolver CreateWorkerCopy()
    {
        var worker = new TriadSolver(new TriadRandomAgent(0));
        worker.Simulation.CopyRulesFrom(Simulation);
        return worker;
    }

    public void InitializeSimulation(IReadOnlyList<TriadRule> primary, IReadOnlyList<TriadRule>? secondary = null)
    {
        Simulation.Initialize(primary, secondary);
    }

    public TriadGameState StartSimulation(TriadDeck deckBlue, TriadDeck deckRed, TriadGameStatus status)
    {
        Agent.OnSimulationStart();
        return Simulation.StartGame(deckBlue, deckRed, status);
    }

    public bool FindNextMove(TriadGameState state, out int cardIndex, out int boardPosition, out TriadSolverResult result)
    {
        return Agent.FindNextMove(this, state, out cardIndex, out boardPosition, out result);
    }

    public void RunSimulation(TriadGameState state, TriadAgent blue, TriadAgent red)
    {
        TriadAgent agent;
        TriadDeckInstance turnDeck;
        TriadOwner owner;
        int cardIndex;
        int boardPosition;
        do
        {
            if (state.Status == TriadGameStatus.InProgressBlue)
            {
                agent = blue;
                turnDeck = state.DeckBlue;
                owner = TriadOwner.Blue;
            }
            else if (state.Status == TriadGameStatus.InProgressRed)
            {
                agent = red;
                turnDeck = state.DeckRed;
                owner = TriadOwner.Red;
            }
            else
            {
                return;
            }
            if (!agent.FindNextMove(this, state, out cardIndex, out boardPosition, out var result))
            {
                return;
            }
        }
        while (Simulation.PlaceCard(state, cardIndex, turnDeck, owner, boardPosition));
    }

    public void FindAvailableActions(TriadGameState state, out int boardMask, out int cardsMask)
    {
        boardMask = 0;
        var board = state.Board;
        for (var i = 0; i < board.Length; i++)
        {
            if (board[i].IsEmpty)
            {
                boardMask |= 1 << i;
            }
        }
        var availableCardMask = state.TurnDeck.AvailableCardMask;
        if (state.ForcedCardIndex >= 0)
        {
            var forcedBit = 1 << state.ForcedCardIndex;
            cardsMask = (((availableCardMask & forcedBit) != 0) ? forcedBit : availableCardMask);
        }
        else
        {
            cardsMask = availableCardMask;
        }
        if (!Simulation.HasFeature(TriadRuleFeatures.FilterNext))
        {
            return;
        }
        var rules = Simulation.Rules;
        for (var j = 0; j < rules.Count; j++)
        {
            rules[j].OnFilterNextCards(state, ref cardsMask);
        }
    }

    public void FindAvailableActions(TriadGameState state, out int boardMask, out int boardCount, out int cardsMask, out int cardsCount)
    {
        FindAvailableActions(state, out boardMask, out cardsMask);
        boardCount = BitOperations.PopCount((uint)boardMask);
        cardsCount = BitOperations.PopCount((uint)cardsMask);
    }
}
