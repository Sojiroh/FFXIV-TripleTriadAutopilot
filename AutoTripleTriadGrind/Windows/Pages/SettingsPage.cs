using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using AutoTripleTriadGrind.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Pages;

internal sealed class SettingsPage
{
    private enum Tab
    {
        General,
        Deck,
        Play,
        PartyInvites,
        GmAlert
    }

    private readonly record struct Entry(Tab Tab, LocString Label, FontAwesomeIcon Icon, LocString Subtitle);

    private static readonly Entry[] entries = new Entry[5]
    {
        new Entry(Tab.General, L.Settings.CatGeneral, FontAwesomeIcon.Cog, L.Settings.CatGeneralSub),
        new Entry(Tab.Deck, L.Deck.Category, FontAwesomeIcon.LayerGroup, L.Deck.CategorySub),
        new Entry(Tab.Play, L.Play.Category, FontAwesomeIcon.Gamepad, L.Play.CategorySub),
        new Entry(Tab.PartyInvites, L.Safety.CatPartyInvites, FontAwesomeIcon.UserSlash, L.Safety.CatPartyInvitesSub),
        new Entry(Tab.GmAlert, L.Safety.CatGmAlert, FontAwesomeIcon.UserSecret, L.Safety.CatGmAlertSub)
    };

    private Tab activeTab;

    private bool resetScroll;

    public void Draw(Plugin plugin)
    {
        var configuration = plugin.Configuration;
        var globalScale = ImGuiHelpers.GlobalScale;
        var navWidth = 196f * globalScale;
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
        {
            using (ImRaii.ChildDisposable nav = ImRaii.Child("##tta_settings_nav", new Vector2(navWidth, -1f), border: false, ImGuiWindowFlags.NoScrollbar))
            {
                if ((bool)nav)
                {
                    DrawNav();
                }
            }
            ImGui.SameLine(0f, 18f * globalScale);
            using (ImRaii.ChildDisposable content = ImRaii.Child("##tta_settings_content", new Vector2(-1f, -1f), border: false, ImGuiWindowFlags.None))
            {
                if ((bool)content)
                {
                    DrawContent(configuration);
                }
            }
        }
    }

    private void DrawNav()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var title = Loc.T(L.Settings.Title);
        using (Fonts.PushTitle())
        {
            TextDraw.At(title, new Vector2(cursorScreenPos.X + 6f * globalScale, cursorScreenPos.Y), Styling.TextStrong);
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, TextDraw.Measure(title).Y + 10f * globalScale));
        }
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (SidebarTab.Draw(Loc.T(entry.Label), entry.Icon, Styling.AccentGlow, activeTab == entry.Tab))
            {
                Select(entry.Tab);
            }
        }
    }

    private void Select(Tab tab)
    {
        if (activeTab == tab)
        {
            return;
        }
        activeTab = tab;
        resetScroll = true;
    }

    private void DrawContent(Configuration configuration)
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }
        var entry = entries[(int)activeTab];
        var globalScale = ImGuiHelpers.GlobalScale;
        using (Motion.PushSwitch("##tta_settings_tab", (int)activeTab))
        {
            using (ImRaii.Group())
            {
                ImGui.Dummy(new Vector2(0f, 2f * globalScale));
                PageHeader.Draw(Loc.T(entry.Label), Loc.T(entry.Subtitle));
                switch (activeTab)
                {
                    case Tab.General:
                        GeneralSettings.Draw(configuration);
                        break;
                    case Tab.Deck:
                        DeckSettings.Draw(configuration);
                        break;
                    case Tab.Play:
                        PlaySettings.Draw(configuration);
                        break;
                    case Tab.PartyInvites:
                        PartyInviteSettings.Draw(configuration);
                        break;
                    case Tab.GmAlert:
                        GmAlertSettings.Draw(configuration);
                        break;
                }
            }
        }
    }
}
