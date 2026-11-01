using ECommons;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class TriadAddons
{
    public const string Request = "TripleTriadRequest";

    public const string DeckSelect = "TripleTriadSelDeck";

    public const string Board = "TripleTriad";

    public const string Result = "TripleTriadResult";

    public unsafe static bool TryGet(string name, out AtkUnitBase* addon, bool requireVisible = true)
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(name, out addon) || addon == null)
        {
            return false;
        }
        return !requireVisible || addon->IsVisible;
    }

    public unsafe static bool IsVisible(string name) => TryGet(name, out _);

    public unsafe static bool IsReady(string name) => TryGet(name, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool TryGetBoard(out AddonTripleTriad* board, bool requireVisible = true)
    {
        board = null;
        if (!TryGet(Board, out var addon, requireVisible))
        {
            return false;
        }
        board = (AddonTripleTriad*)addon;
        return true;
    }

    public static bool AnyMatchWindowVisible() => IsVisible(Request) || IsVisible(DeckSelect) || IsVisible(Board) || IsVisible(Result);
}
