using System;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Game.Ops;

internal static class TriadDialog
{
    public enum Step : byte
    {
        Nothing,
        Handled,
        ChoseChallenge,
        UnknownMenu,
        QuestOffered
    }

    private const string TalkAddon = "Talk";

    private const string SelectStringAddon = "SelectString";

    private const string SelectIconStringAddon = "SelectIconString";

    private const string SelectYesnoAddon = "SelectYesno";

    private const string JournalAcceptAddon = "JournalAccept";

    private const uint MenuListNodeId = 3u;

    private const int ClickThrottleMs = 400;

    private const string TalkThrottleKey = "TTA.Triad.Talk";

    private const string MenuThrottleKey = "TTA.Triad.Menu";

    private const string YesnoThrottleKey = "TTA.Triad.Yesno";

    private const string QuestThrottleKey = "TTA.Triad.Quest";

    private const uint TriadChallengeAddonRowId = 9184u;

    private static readonly uint[] triadEntryIcons = new uint[3] { 60091u, 61721u, 61723u };

    private static readonly string[] fallbackEntryWords = new string[7] { "Triple Triad", "Triple-Triad", "triad", "triade", "triplo", "トリプル", "幻卡" };

    private static string[]? triadEntryWords;

    public unsafe static Step Advance(bool towardChallenge = true)
    {
        if (TryGetReady(JournalAcceptAddon, out var addon))
        {
            if (EzThrottler.Throttle(QuestThrottleKey, ClickThrottleMs))
            {
                DeclineQuest(addon);
            }
            return Step.QuestOffered;
        }
        if (TryGetReady(SelectYesnoAddon, out var yesnoAddon))
        {
            if (!IsOwnedByTriad(yesnoAddon))
            {
                return Step.UnknownMenu;
            }
            if (EzThrottler.Throttle(YesnoThrottleKey, ClickThrottleMs))
            {
                var selectYesno = new AddonMaster.SelectYesno(yesnoAddon);
                if (towardChallenge)
                {
                    selectYesno.Yes();
                }
                else
                {
                    selectYesno.No();
                }
            }
            return Step.Handled;
        }
        if (TryGetReady(SelectIconStringAddon, out var iconMenuAddon))
        {
            if (!towardChallenge)
            {
                return SelectLastEntry(iconMenuAddon, isIconMenu: true);
            }
            return SelectTriadEntry(iconMenuAddon, isIconMenu: true);
        }
        if (TryGetReady(SelectStringAddon, out var stringMenuAddon))
        {
            if (!towardChallenge)
            {
                return SelectLastEntry(stringMenuAddon, isIconMenu: false);
            }
            return SelectTriadEntry(stringMenuAddon, isIconMenu: false);
        }
        if (TryGetReady(TalkAddon, out var talkAddon))
        {
            if (EzThrottler.Throttle(TalkThrottleKey, ClickThrottleMs))
            {
                new AddonMaster.Talk(talkAddon).Click();
            }
            return Step.Handled;
        }
        return Step.Nothing;
    }

    public unsafe static bool AnyOpen()
    {
        if (!TryGetReady(TalkAddon, out var addon) && !TryGetReady(SelectStringAddon, out addon) && !TryGetReady(SelectIconStringAddon, out addon))
        {
            return TryGetReady(SelectYesnoAddon, out addon);
        }
        return true;
    }

    private unsafe static void DeclineQuest(AtkUnitBase* questOffer)
    {
        RunLog.Info("Declining the quest offered in place of the challenge.");
        var journalAccept = new AddonMaster.JournalAccept(questOffer);
        if (journalAccept.DeclineButton == null)
        {
            questOffer->Close(fireCallback: true);
        }
        else
        {
            journalAccept.Decline();
        }
    }

    private unsafe static Step SelectTriadEntry(AtkUnitBase* menu, bool isIconMenu)
    {
        var entryIndex = FindTriadEntry(menu, isIconMenu);
        if (entryIndex < 0)
        {
            return Step.UnknownMenu;
        }
        if (!EzThrottler.Throttle(MenuThrottleKey, ClickThrottleMs))
        {
            return Step.Handled;
        }
        RunLog.Info($"Choosing menu entry {entryIndex} ({EntryText(menu, isIconMenu, entryIndex)}).");
        Callback.Fire(menu, true, entryIndex);
        return Step.ChoseChallenge;
    }

    private unsafe static Step SelectLastEntry(AtkUnitBase* menu, bool isIconMenu)
    {
        var entryCount = EntryCount(menu, isIconMenu);
        if (entryCount == 0)
        {
            return Step.UnknownMenu;
        }
        if (EzThrottler.Throttle(MenuThrottleKey, ClickThrottleMs))
        {
            Callback.Fire(menu, true, entryCount - 1);
        }
        return Step.Handled;
    }

    private unsafe static int FindTriadEntry(AtkUnitBase* menu, bool isIconMenu)
    {
        var menuList = menu->GetComponentListById(MenuListNodeId);
        var listItemCount = ((menuList == null) ? 0 : menuList->GetItemCount());
        for (int i = 0; i < listItemCount; i++)
        {
            if (Array.IndexOf(triadEntryIcons, menuList->ItemRendererList[i].IconId) >= 0)
            {
                return i;
            }
        }
        var entryCount = EntryCount(menu, isIconMenu);
        for (int j = 0; j < entryCount; j++)
        {
            if (IsTriadText(EntryText(menu, isIconMenu, j)))
            {
                return j;
            }
        }
        if (entryCount != 1)
        {
            return -1;
        }
        return 0;
    }

    private unsafe static int EntryCount(AtkUnitBase* menu, bool isIconMenu)
    {
        if (!isIconMenu)
        {
            return new AddonMaster.SelectString(menu).EntryCount;
        }
        return new AddonMaster.SelectIconString(menu).EntryCount;
    }

    private unsafe static string EntryText(AtkUnitBase* menu, bool isIconMenu, int index)
    {
        if (!isIconMenu)
        {
            return new AddonMaster.SelectString(menu).Entries[index].Text;
        }
        return new AddonMaster.SelectIconString(menu).Entries[index].Text;
    }

    private static bool IsTriadText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        var words = triadEntryWords ?? (triadEntryWords = ReadTriadEntryWords());
        for (int i = 0; i < words.Length; i++)
        {
            if (text.Contains(words[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string[] ReadTriadEntryWords()
    {
        string? localizedName = Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(TriadChallengeAddonRowId)?.Text.ExtractText();
        if (!string.IsNullOrWhiteSpace(localizedName))
        {
            return [localizedName, .. fallbackEntryWords];
        }
        return fallbackEntryWords;
    }

    private unsafe static bool IsOwnedByTriad(AtkUnitBase* addon)
    {
        var raptureAtkModule = RaptureAtkModule.Instance();
        var agentModule = AgentModule.Instance();
        if (raptureAtkModule == null || agentModule == null || !raptureAtkModule->AddonCallbackMapping.TryGetValue(addon->Id, out var callbackEntry, copyCtor: false))
        {
            return false;
        }
        return callbackEntry.AgentInterface == agentModule->GetAgentByInternalId(AgentId.TripleTriad);
    }

    private unsafe static bool TryGetReady(string name, out AtkUnitBase* addon)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(name, out addon))
        {
            return GenericHelpers.IsAddonReady(addon);
        }
        return false;
    }
}
