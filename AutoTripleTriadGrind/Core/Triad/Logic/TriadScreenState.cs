using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;

namespace AutoTripleTriadGrind.Core.Triad.Logic;

public sealed class TriadScreenState
{
    public readonly ushort[] BlueHand = TriadDeckInstanceScreen.NewHand();

    public readonly ushort[] RedHand = TriadDeckInstanceScreen.NewHand();

    public readonly ushort[] Board = NewBoardCards();

    public readonly TriadOwner[] BoardOwners = new TriadOwner[TriadGameState.BoardCells];

    public readonly List<TriadRule> Rules = new(4);

    public ushort ForcedBlueCard = TriadCards.None;

    public bool PlayerTurn;

    public static ushort[] NewBoardCards()
    {
        var cards = new ushort[TriadGameState.BoardCells];
        Array.Fill(cards, TriadCards.None);
        return cards;
    }
}
