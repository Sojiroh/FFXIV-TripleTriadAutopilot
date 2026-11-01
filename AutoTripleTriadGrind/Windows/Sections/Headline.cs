using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Stats;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class Headline
{
    private const float GreetingGap = 8f;

    private const float DetailGap = 6f;

    private const float RightGap = 18f;

    private const float PluginsButtonHeight = 30f;

    public static bool Draw(Configuration configuration, AutoTriadController controller, RunHistory history)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var info = ReadyState.Resolve(configuration, controller);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var cursorY = cursorScreenPos.Y;
        var (icon, color, greeting) = Greeting();
        using (Fonts.PushCaption())
        {
            var greetingSize = TextDraw.Measure(greeting);
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(cursorScreenPos.X + 1f * globalScale, cursorY + (greetingSize.Y - iconSize.Y) * 0.5f), color);
            TextDraw.At(greeting, new Vector2(cursorScreenPos.X + iconSize.X + GreetingGap * globalScale, cursorY), Styling.TextDim);
            cursorY += greetingSize.Y + GreetingGap * globalScale;
        }
        float titleLineHeight;
        using (Fonts.PushTitle())
        {
            titleLineHeight = ImGui.GetTextLineHeight();
        }
        var detailLineHeight = ImGui.GetTextLineHeight();
        var blockHeight = titleLineHeight + DetailGap * globalScale + detailLineHeight;
        var midY = cursorY + blockHeight * 0.5f;
        var rightWidth = DrawRightColumn(info, history, cursorScreenPos.X + availableWidth, midY, out var openPlugins);
        var maxWidth = availableWidth - rightWidth - RightGap * globalScale;
        using (Fonts.PushTitle())
        {
            TextDraw.At(TextDraw.Truncate(info.Title + Loc.T(L.Triad.SentenceEnd), maxWidth), new Vector2(cursorScreenPos.X, cursorY), Styling.TextStrong);
        }
        cursorY += titleLineHeight + DetailGap * globalScale;
        TextDraw.At(TextDraw.Truncate(info.Detail, maxWidth), new Vector2(cursorScreenPos.X, cursorY), Styling.TextDim);
        cursorY += detailLineHeight;
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availableWidth, cursorY - cursorScreenPos.Y));
        return openPlugins;
    }

    private static (FontAwesomeIcon Icon, Vector4 Color, string Greeting) Greeting()
        => DateTime.Now.Hour switch
        {
            >= 5 and < 12 => (Icon: FontAwesomeIcon.Sun, Color: Styling.AccentAmber, Greeting: Loc.T(L.Shell.GreetingMorning)),
            >= 12 and < 17 => (Icon: FontAwesomeIcon.Sun, Color: Styling.AccentAmber, Greeting: Loc.T(L.Shell.GreetingAfternoon)),
            >= 17 and < 22 => (Icon: FontAwesomeIcon.CloudMoon, Color: Styling.AccentGlowSoft, Greeting: Loc.T(L.Shell.GreetingEvening)),
            _ => (Icon: FontAwesomeIcon.Moon, Color: Styling.AccentBlue, Greeting: Loc.T(L.Shell.GreetingNight)),
        };

    private static float DrawRightColumn(ReadyState.Info info, RunHistory history, float rightX, float midY, out bool openPlugins)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        openPlugins = false;
        if (info.Kind == ReadyState.Kind.SetupNeeded)
        {
            var label = Loc.T(L.Triad.OpenPlugins);
            var buttonWidth = PillButton.Width(label, FontAwesomeIcon.Plug);
            ImGui.SetCursorScreenPos(new Vector2(rightX - buttonWidth, midY - PluginsButtonHeight * globalScale * 0.5f));
            openPlugins = PillButton.Draw("##tta_open_plugins", label, Styling.AccentRose, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, enabled: true, PluginsButtonHeight);
            return buttonWidth;
        }
        var (title, detail) = LastRun(history);
        using (Fonts.PushCaption())
        {
            var titleSize = TextDraw.Measure(title);
            var detailSize = TextDraw.Measure(detail);
            var lineGap = 3f * globalScale;
            var topY = midY - (titleSize.Y + lineGap + detailSize.Y) * 0.5f;
            TextDraw.At(title, new Vector2(rightX - titleSize.X, topY), Styling.TextSecondary);
            TextDraw.At(detail, new Vector2(rightX - detailSize.X, topY + titleSize.Y + lineGap), Styling.TextDim);
            return MathF.Max(titleSize.X, detailSize.X);
        }
    }

    private static (string Title, string Detail) LastRun(RunHistory history)
    {
        var records = history.Records;
        if (records.Count == 0)
        {
            return (Title: Loc.T(L.Triad.NoRunsYet), Detail: Loc.T(L.Triad.StatsAppearHere));
        }
        var runRecord = records[0];
        return (Title: Loc.T(L.Triad.LastRun, runRecord.CardsObtained.Count), Detail: Loc.T(L.Triad.LastRunDetail, Formatting.Elapsed(runRecord.Duration), runRecord.MatchesWon, runRecord.MatchesPlayed));
    }
}
