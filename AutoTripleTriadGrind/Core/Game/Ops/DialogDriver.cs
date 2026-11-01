using ECommons;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Game.Ops;

internal static class DialogDriver
{
    private const string SelectYesnoAddon = "SelectYesno";

    private const string SelectStringAddon = "SelectString";

    private const string TalkAddon = "Talk";

    private const int ClickThrottleMs = 500;

    private const string SelectStringThrottleKey = "TTA.Dialog.SelectString";

    private const string SelectYesnoThrottleKey = "TTA.Dialog.SelectYesno";

    private const string TalkThrottleKey = "TTA.Dialog.Talk";

    public unsafe static bool ConfirmationOpen()
    {
        AtkUnitBase* addon;
        return AddonReady(SelectYesnoAddon, out addon);
    }

    public unsafe static bool Confirm()
    {
        if (!AddonReady(SelectYesnoAddon, out var addon))
        {
            return false;
        }
        Diag("Dialog: answering Yes to the confirmation.");
        new AddonMaster.SelectYesno(addon).Yes();
        return true;
    }

    public unsafe static bool AnyOpen()
    {
        if (!AddonReady(TalkAddon, out var addon) && !AddonReady(SelectYesnoAddon, out addon))
        {
            return AddonReady(SelectStringAddon, out addon);
        }
        return true;
    }

    public unsafe static bool AdvanceAccepting()
    {
        if (AddonReady(SelectStringAddon, out var addon))
        {
            if (EzThrottler.Throttle(SelectStringThrottleKey))
            {
                SelectFirstEntry(addon);
            }
            return true;
        }
        if (AddonReady(SelectYesnoAddon, out var yesnoAddon))
        {
            if (EzThrottler.Throttle(SelectYesnoThrottleKey))
            {
                Diag("Dialog: answering Yes.");
                new AddonMaster.SelectYesno(yesnoAddon).Yes();
            }
            return true;
        }
        if (AddonReady(TalkAddon, out var talkAddon))
        {
            if (EzThrottler.Throttle(TalkThrottleKey))
            {
                Diag("Dialog: advancing the talk window.");
                new AddonMaster.Talk(talkAddon).Click();
            }
            return true;
        }
        return false;
    }

    private unsafe static void SelectFirstEntry(AtkUnitBase* addon)
    {
        var selectString = new AddonMaster.SelectString(addon);
        if (selectString.EntryCount == 0)
        {
            return;
        }
        var entry = selectString.Entries[0];
        Diag($"Dialog: choosing '{entry.Text}' from a menu of {selectString.EntryCount}.");
        entry.Select();
    }

    private unsafe static bool AddonReady(string name, out AtkUnitBase* addon)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(name, out addon))
        {
            return GenericHelpers.IsAddonReady(addon);
        }
        return false;
    }

    private static void Diag(string message)
    {
        RunLog.Info(message);
    }
}
