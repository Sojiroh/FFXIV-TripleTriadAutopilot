using System;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic;
using AutoTripleTriadGrind.Core.Triad.Match;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class TriadMatchOps
{
    private const uint ChallengeButtonId = 41u;

    private const int PlaceCardEvent = 14;

    private const int ResultRematch = 0;

    private const int WindowQuit = 1;

    private static readonly uint[] deckConfirmButtons = new uint[2] { 5u, 1u };

    public unsafe static bool TryReadRegionalRules(Span<TriadRuleId> regional) => TriadAddons.TryGet(TriadAddons.Request, out var addon) && RequestReader.TryReadRegionalRules(addon, regional);

    public unsafe static void ClickChallenge()
    {
        if (!TriadAddons.TryGet(TriadAddons.Request, out var addon) || !GenericHelpers.IsAddonReady(addon))
        {
            return;
        }
        var challengeButton = addon->GetComponentButtonById(ChallengeButtonId);
        if (challengeButton == null || challengeButton->AtkResNode == null || !challengeButton->AtkResNode->IsVisible())
        {
            return;
        }
        challengeButton->ClickAddonButton(addon);
        addon->Update(0f);
    }

    public unsafe static bool DeckChosen() => TriadAddons.TryGetBoard(out var board, requireVisible: false) && BoardReader.HandsPopulated(board) && !TriadAddons.IsVisible(TriadAddons.DeckSelect);

    public unsafe static void ChooseDeck(int slot, int attempt)
    {
        if (!TriadAddons.TryGet(TriadAddons.DeckSelect, out var addon, requireVisible: false))
        {
            return;
        }
        if (attempt % 2 == 0)
        {
            var callbackValue = stackalloc AtkValue[1];
            *callbackValue = new AtkValue
            {
                Type = AtkValueType.Int,
                Int = slot
            };
            addon->FireCallback(1u, callbackValue, close: true);
        }
        else
        {
            ClickFirstButton(addon);
        }
        addon->Update(0f);
    }

    public unsafe static bool TryReadBoard(TriadScreenState screen, out BoardFrame frame)
    {
        if (!TriadAddons.TryGetBoard(out var board))
        {
            frame = default;
            return false;
        }
        frame = BoardReader.Read(board, screen);
        return true;
    }

    public unsafe static void PlaceCard(int handSlot, int cell)
    {
        if (!TriadAddons.TryGetBoard(out var board))
        {
            return;
        }
        Callback.Fire(&board->AtkUnitBase, true, PlaceCardEvent, (uint)(cell + (handSlot << 16)));
        board->AtkUnitBase.Update(0f);
        board->TurnState = TurnState.Waiting;
    }

    public static bool PlayAnyCard(TriadScreenState screen)
    {
        var handSlot = -1;
        for (var i = 0; i < screen.BlueHand.Length; i++)
        {
            var card = screen.BlueHand[i];
            if (card is not (TriadCards.Hidden or TriadCards.None))
            {
                handSlot = i;
                break;
            }
        }
        var cell = Array.IndexOf(screen.Board, TriadCards.None);
        if (handSlot < 0 || cell < 0)
        {
            return false;
        }
        PlaceCard(handSlot, cell);
        return true;
    }

    public unsafe static bool ResultReady() => TriadAddons.TryGet(TriadAddons.Result, out var addon) && ResultReader.IsReady(addon);

    public unsafe static MatchOutcome ReadOutcome()
    {
        if (!TriadAddons.TryGet(TriadAddons.Result, out var addon))
        {
            return MatchOutcome.Unknown;
        }
        return ResultReader.ReadOutcome(addon);
    }

    public unsafe static void Rematch()
    {
        if (!TriadAddons.TryGet(TriadAddons.Result, out var addon) || !ResultReader.IsReady(addon))
        {
            return;
        }
        addon->FireCallbackInt(ResultRematch);
        addon->Update(0f);
    }

    public unsafe static bool CloseOneWindow()
    {
        if (TriadAddons.TryGet(TriadAddons.Result, out var resultAddon))
        {
            resultAddon->FireCallbackInt(WindowQuit);
            resultAddon->Update(0f);
            return true;
        }
        if (TriadAddons.TryGet(TriadAddons.Request, out var requestAddon))
        {
            requestAddon->FireCallbackInt(WindowQuit);
            requestAddon->Update(0f);
            return true;
        }
        if (TriadAddons.TryGet(TriadAddons.DeckSelect, out var deckSelectAddon))
        {
            deckSelectAddon->Close(fireCallback: true);
            return true;
        }
        return false;
    }

    private unsafe static void ClickFirstButton(AtkUnitBase* addon)
    {
        for (var i = 0; i < deckConfirmButtons.Length; i++)
        {
            var button = addon->GetComponentButtonById(deckConfirmButtons[i]);
            if (button != null && button->IsEnabled && button->AtkResNode != null && button->AtkResNode->IsVisible())
            {
                button->ClickAddonButton(addon);
                break;
            }
        }
    }
}
