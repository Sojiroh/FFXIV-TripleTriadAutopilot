using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Logic;
using AutoTripleTriadGrind.Core.Triad.Logic.Rules;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class BoardReader
{
    private const int PlayerTurnValueIndex = 23;

    private const int RootChildren = 12;

    private const int RulesNodeIndex = 4;

    private const int RulesChildren = 5;

    private const int PvpNodeIndex = 11;

    private const int MaxRules = 4;

    private const byte LockedTint = 100;

    private static readonly string[] turnBannerMarkers = new string[10] { " TURN", " TOUR ", " TOUR DE ", " ZUG", " AM ZUG", " TURNO", " VEZ DE ", "のターン", "回合", " ход" };

    public unsafe static BoardFrame Read(AddonTripleTriad* board, TriadScreenState screen)
    {
        var atkUnitBase = &board->AtkUnitBase;
        var turnState = (byte)board->TurnState;
        var playerTurn = atkUnitBase->AtkValuesCount > PlayerTurnValueIndex && atkUnitBase->AtkValues[PlayerTurnValueIndex].Type == AtkValueType.Int && atkUnitBase->AtkValues[PlayerTurnValueIndex].Int == 1;
        var bannerVisible = NodeWalk.AnyVisibleText(atkUnitBase->RootNode, IsTurnBanner);
        var failed = false;
        var cardsInHands = 0;
        var forcedBlueCard = TriadCards.None;
        var isActiveTurn = turnState == 2;
        for (var i = 0; i < 5; i++)
        {
            screen.BlueHand[i] = ReadCard(board->BlueDeck[i], out var locked, ref failed);
            screen.RedHand[i] = ReadCard(board->RedDeck[i], out _, ref failed);
            cardsInHands += (board->BlueDeck[i].HasCard ? 1 : 0);
            cardsInHands += (board->RedDeck[i].HasCard ? 1 : 0);
            if (isActiveTurn && board->BlueDeck[i].HasCard && !locked)
            {
                forcedBlueCard = screen.BlueHand[i];
            }
        }
        for (var j = 0; j < TriadGameState.BoardCells; j++)
        {
            var card = board->Board[j];
            screen.Board[j] = ReadCard(card, out _, ref failed);
            screen.BoardOwners[j] = (byte)card.CardOwner switch
            {
                1 => TriadOwner.Blue,
                2 => TriadOwner.Red,
                _ => TriadOwner.Unknown,
            };
        }
        screen.ForcedBlueCard = forcedBlueCard;
        failed |= !ReadRules(atkUnitBase->RootNode, screen.Rules);
        var isPvP = NodeWalk.IsVisible(NodeWalk.Child(atkUnitBase->RootNode, PvpNodeIndex, RootChildren));
        var canAct = isActiveTurn || (((turnState == 1) & playerTurn) && !bannerVisible);
        screen.PlayerTurn = canAct;
        return new BoardFrame(canAct, playerTurn, turnState, bannerVisible, cardsInHands, isPvP, failed);
    }

    public unsafe static bool HandsPopulated(AddonTripleTriad* board)
    {
        var blueCards = 0;
        var redCards = 0;
        for (var i = 0; i < 5; i++)
        {
            blueCards += (board->BlueDeck[i].HasCard ? 1 : 0);
            redCards += (board->RedDeck[i].HasCard ? 1 : 0);
        }
        return blueCards > 0 && redCards > 0;
    }

    private unsafe static ushort ReadCard(AddonTripleTriad.TripleTriadCard card, out bool locked, ref bool failed)
    {
        locked = false;
        if (!card.HasCard)
        {
            return TriadCards.None;
        }
        if (card.NumSideU == 0)
        {
            return TriadCards.Hidden;
        }
        var tintNode = NodeWalk.ComponentChild(NodeWalk.ComponentChild(card.CardDropControl, 1, 3), 0, 2);
        locked = tintNode != null && tintNode->MultiplyRed < LockedTint;
        string? texturePath = NodeWalk.TexturePath(NodeWalk.ComponentChild(tintNode, 3, 21));
        var cardId = TriadCardMatcher.Match(card.NumSideU, card.NumSideL, card.NumSideD, card.NumSideR, (byte)card.CardType, card.CardRarity, texturePath);
        if (cardId == TriadCards.None)
        {
            failed = true;
        }
        return cardId;
    }

    private unsafe static bool ReadRules(AtkResNode* root, List<TriadRule> rules)
    {
        rules.Clear();
        var rulesNode = NodeWalk.Child(root, RulesNodeIndex, RootChildren);
        var childCount = NodeWalk.ChildCount(rulesNode);
        if (childCount == 0)
        {
            return true;
        }
        var allParsed = true;
        var ruleCount = ((childCount == RulesChildren) ? MaxRules : childCount);
        for (var i = 0; i < ruleCount; i++)
        {
            string? ruleText = NodeWalk.Text(NodeWalk.Child(rulesNode, (childCount == RulesChildren) ? (4 - i) : i, childCount));
            if (!string.IsNullOrWhiteSpace(ruleText))
            {
                var rule = TriadRuleParser.Parse(ruleText);
                if (rule != null)
                {
                    rules.Add(rule);
                }
                else
                {
                    allParsed = false;
                }
            }
        }
        return allParsed;
    }

    private static bool IsTurnBanner(string text)
    {
        if (text.Length > 96)
        {
            return false;
        }
        for (var i = 0; i < turnBannerMarkers.Length; i++)
        {
            if (text.Contains(turnBannerMarkers[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
