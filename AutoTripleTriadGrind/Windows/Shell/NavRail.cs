using System.Numerics;
using AutoTripleTriadGrind.Core;
using AutoTripleTriadGrind.Core.External;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Shell;

internal static class NavRail
{
    private readonly record struct Entry(AppWindow.Page Page, FontAwesomeIcon Icon, string Id, LocString Label);

    private const float TopPad = 12f;

    private const float Gap = 8f;

    private static readonly Entry[] entries = new Entry[7]
    {
        new Entry(AppWindow.Page.Triad, FontAwesomeIcon.ThLarge, "##tta_nav_triad", L.Shell.NavTriad),
        new Entry(AppWindow.Page.Settings, FontAwesomeIcon.SlidersH, "##tta_nav_settings", L.Shell.NavSettings),
        new Entry(AppWindow.Page.History, FontAwesomeIcon.ChartLine, "##tta_nav_history", L.Shell.NavHistory),
        new Entry(AppWindow.Page.Plugins, FontAwesomeIcon.Plug, "##tta_nav_plugins", L.Shell.NavPlugins),
        new Entry(AppWindow.Page.Log, FontAwesomeIcon.Terminal, "##tta_nav_log", L.Shell.NavLog),
        new Entry(AppWindow.Page.Changelog, FontAwesomeIcon.Newspaper, "##tta_nav_changelog", L.Shell.NavChangelog),
        new Entry(AppWindow.Page.About, FontAwesomeIcon.InfoCircle, "##tta_nav_about", L.Shell.NavAbout)
    };

    public static AppWindow.Page? Draw(AppWindow.Page current, Plugin plugin)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var button = Layout.RailButton * globalScale;
        var gap = Gap * globalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var buttonX = cursorScreenPos.X + (availableWidth - button) * 0.5f;
        var top = cursorScreenPos.Y + TopPad * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var currentIndex = 0;
        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].Page == current)
            {
                currentIndex = i;
            }
        }
        var indicatorIndex = Motion.Approach(Motion.Key("##tta_rail_indicator"), currentIndex, 16f);
        var indicatorY = top + (button + gap) * indicatorIndex;
        var indicatorMin = new Vector2(buttonX, indicatorY);
        var indicatorMax = indicatorMin + new Vector2(button, button);
        Paint.Glass(windowDrawList, indicatorMin, indicatorMax, 12f * globalScale, Styling.AccentGlow, 0.3f);
        Paint.Fill(windowDrawList, new Vector2(cursorScreenPos.X, indicatorY + button * 0.25f), new Vector2(cursorScreenPos.X + 3f * globalScale, indicatorY + button * 0.75f), Styling.AccentGlow, 2f * globalScale);
        var missingPlugins = !ExternalPlugins.AllRequiredInstalled();
        var running = plugin.Controller.Running;
        var hasUnseenChangelog = plugin.Configuration.HasUnseenChangelog;
        AppWindow.Page? clickedPage = null;
        for (var j = 0; j < entries.Length; j++)
        {
            var entry = entries[j];
            var buttonY = top + (button + gap) * j;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, buttonY));
            var hit = Hit.Area(entry.Id, new Vector2(button, button));
            var hover = Motion.Hover(Motion.Key(entry.Id), hit.Hovered);
            var selected = j == currentIndex;
            if (!selected && hover > 0.01f)
            {
                Paint.Fill(windowDrawList, new Vector2(buttonX, buttonY), new Vector2(buttonX + button, buttonY + button), Styling.WithAlpha(Styling.Surface2, 0.8f * hover), 12f * globalScale);
            }
            var center = new Vector2(buttonX + button * 0.5f, buttonY + button * 0.5f);
            var color = (selected ? Styling.TextStrong : Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover));
            TextDraw.IconCentered(entry.Icon, center, color);
            DrawBadge(windowDrawList, entry.Page, current, center, button, missingPlugins, running, hasUnseenChangelog);
            if (hit.Hovered)
            {
                Tooltip.Show(Loc.T(entry.Label));
            }
            if (hit.Clicked)
            {
                clickedPage = entry.Page;
            }
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availableWidth, TopPad * globalScale + (button + gap) * entries.Length));
        return clickedPage;
    }

    private static void DrawBadge(ImDrawListPtr dl, AppWindow.Page page, AppWindow.Page current, Vector2 center, float button, bool missingPlugins, bool running, bool unseenChangelog)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var badgeCenter = center + new Vector2(button * 0.3f, -button * 0.3f);
        var badgeRadius = 3.5f * globalScale;
        if ((page == AppWindow.Page.Plugins) & missingPlugins)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.AccentRose));
        }
        else if ((page == AppWindow.Page.Triad) & running)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.PulseColor(Styling.AccentBlue, Styling.AccentBlueSoft)));
        }
        else if ((page == AppWindow.Page.Changelog && current != AppWindow.Page.Changelog) & unseenChangelog)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.PulseColor(Styling.AccentGlow, Styling.AccentGlowSoft)));
        }
        else if (page == AppWindow.Page.Log && current != AppWindow.Page.Log)
        {
            if (RunLog.Unseen is { } level)
            {
                dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
                dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col((level == RunLogLevel.Error) ? Styling.AccentRose : Styling.AccentAmber));
            }
        }
    }
}
