using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Stats;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Pages;

internal sealed class HistoryPage
{
    private const int ChartRuns = 24;

    private const int TileCount = 5;

    private const float PadX = 14f;

    private const float MetricWidth = 64f;

    private const float ModeIconColumn = 18f;

    private const float ConfirmSlide = 12f;

    private const string NoValue = "-";

    private bool confirmClear;

    public void Draw(Plugin plugin)
    {
        var history = plugin.History;
        var lifetime = history.Lifetime;
        var subtitle = (history.Records.Count == 0) ? Loc.T(L.History.Empty) : Loc.Plural(L.History.Summary, history.Records.Count, Formatting.Elapsed(lifetime.Duration), (lifetime.WinRate * 100.0).ToString("F0", Loc.Culture));
        PageHeader.Draw(Loc.T(L.History.Title), subtitle);
        DrawLifetime(lifetime);
        Styling.VSpace(14f);
        if (history.Records.Count == 0)
        {
            DrawEmptyState();
            return;
        }
        Label(Loc.T(L.History.CardsPerRun));
        DrawChart(history);
        Styling.VSpace(12f);
        Label(Loc.T(L.History.RecentRuns));
        var records = history.Records;
        for (var i = 0; i < records.Count; i++)
        {
            DrawRow(records[i], i);
            Styling.VSpace(2f);
        }
        Styling.VSpace(8f);
        DrawClearControl(history);
    }

    private static void Label(string text)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var titleSize = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, cursorScreenPos, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, titleSize.Y + 8f * globalScale));
    }

    private static void DrawLifetime(RunHistory.LifetimeTotals totals)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var gap = 8f * globalScale;
        var width = (ImGui.GetContentRegionAvail().X - gap * 4f) / 5f;
        StatTile.Draw(Loc.T(L.History.TileRuns), totals.Runs.ToString("N0", Loc.Culture), null, Styling.AccentGlow, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileMatches), totals.Matches.ToString("N0", Loc.Culture), null, Styling.AccentBlue, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileWins), totals.Wins.ToString("N0", Loc.Culture), null, Styling.AccentMint, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileCards), totals.Cards.ToString("N0", Loc.Culture), null, Styling.AccentNebula, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileTime), Formatting.Elapsed(totals.Duration), null, Styling.AccentAmber, width);
    }

    private static void DrawEmptyState()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 110f * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        Paint.Surface(ImGui.GetWindowDrawList(), cursorScreenPos, max, 10f * globalScale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);
        var center = new Vector2((cursorScreenPos.X + max.X) * 0.5f, cursorScreenPos.Y + size.Y * 0.42f);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.History, Styling.TextMuted, 26f * globalScale);
        TextDraw.Center(Loc.T(L.History.NoRuns), center.X, center.Y + 22f * globalScale, Styling.TextMuted);
        ImGui.Dummy(size);
    }

    private static void DrawChart(RunHistory history)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var records = history.Records;
        var count = Math.Min(24, records.Count);
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 96f * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var cardMax = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Surface(windowDrawList, cursorScreenPos, cardMax, 10f * globalScale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);
        var padX = PadX * globalScale;
        var plotMin = new Vector2(cursorScreenPos.X + padX, cursorScreenPos.Y + 28f * globalScale);
        var plotMax = new Vector2(cardMax.X - padX, cardMax.Y - 12f * globalScale);
        var plotWidth = plotMax.X - plotMin.X;
        var plotHeight = plotMax.Y - plotMin.Y;
        var peak = 1;
        for (var i = 0; i < count; i++)
        {
            peak = Math.Max(peak, records[i].CardsObtained.Count);
        }
        using (Fonts.PushCaption())
        {
            TextDraw.At(Loc.Plural(L.History.ChartRange, count), new Vector2(plotMin.X, cursorScreenPos.Y + 9f * globalScale), Styling.TextMuted);
            TextDraw.Right(Loc.T(L.History.ChartPeak, peak), plotMax.X, cursorScreenPos.Y + 9f * globalScale, Styling.TextMuted);
        }
        windowDrawList.AddLine(new Vector2(plotMin.X, plotMax.Y), new Vector2(plotMax.X, plotMax.Y), Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.7f)), 1f);
        var barGap = 2f * globalScale;
        var barWidth = MathF.Max(2f * globalScale, (plotWidth - barGap * (count - 1)) / count);
        var barStep = barWidth + barGap;
        var hoveredBar = -1;
        if (Hit.HoveringRect(plotMin, plotMax))
        {
            hoveredBar = (int)((ImGui.GetMousePos().X - plotMin.X) / barStep);
            if (hoveredBar >= count)
            {
                hoveredBar = -1;
            }
        }
        var rounding = MathF.Min(4f * globalScale, barWidth * 0.5f);
        for (var j = 0; j < count; j++)
        {
            var record = records[count - 1 - j];
            var barHeight = MathF.Max(2f * globalScale, plotHeight * record.CardsObtained.Count / peak);
            var pMin = new Vector2(plotMin.X + barStep * j, plotMax.Y - barHeight);
            var pMax = new Vector2(pMin.X + barWidth, plotMax.Y);
            var color = (j == hoveredBar) ? Styling.AccentGlowSoft : Styling.AccentGlow;
            windowDrawList.AddRectFilled(pMin, pMax, Paint.Col(color), rounding, ImDrawFlags.RoundCornersTop);
        }
        if (hoveredBar >= 0)
        {
            Tooltip.Show(ChartTooltip(records[count - 1 - hoveredBar]));
        }
        ImGui.Dummy(size);
    }

    private static string ChartTooltip(RunRecord record)
    {
        var relative = RelativeTime(record.EndedAtUtc);
        var duration = Formatting.Elapsed(record.Duration);
        return Loc.T(L.History.ChartTooltip, relative, record.CardsObtained.Count, record.MatchesPlayed, duration);
    }

    private static void DrawRow(RunRecord record, int index)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 60f * globalScale);
        if (!ImGui.IsRectVisible(size))
        {
            ImGui.Dummy(size);
            return;
        }
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        ImGui.PushID(index + 1);
        var hit = Hit.Area("##run", size, enabled: true, handCursor: false);
        var hover = Motion.Hover(Motion.Key("##run"), hit.Hovered);
        ImGui.PopID();
        Paint.Glass(windowDrawList, cursorScreenPos, max, 10f * globalScale, Styling.AccentGlow, 0.02f, hover);
        var padX = PadX * globalScale;
        var midY = cursorScreenPos.Y + size.Y * 0.5f;
        var textX = DrawModeIcon(record.Mode, cursorScreenPos.X + padX, midY);
        var relative = RelativeTime(record.EndedAtUtc);
        var detail = Loc.T(L.History.RowDetail, ModeLabel(record.Mode), Formatting.Elapsed(record.Duration));
        var relativeSize = TextDraw.Measure(relative);
        Vector2 detailSize;
        using (Fonts.PushCaption())
        {
            detailSize = TextDraw.Measure(detail);
        }
        var textTop = midY - (relativeSize.Y + 3f * globalScale + detailSize.Y) * 0.5f;
        TextDraw.At(relative, new Vector2(textX, textTop), Styling.TextStrong);
        using (Fonts.PushCaption())
        {
            TextDraw.At(detail, new Vector2(textX, textTop + relativeSize.Y + 3f * globalScale), Styling.TextDim);
        }
        var metricWidth = MetricWidth * globalScale;
        var cardsX = max.X - padX - metricWidth;
        DrawMetric(cardsX, midY, metricWidth, Metric(record.CardsObtained.Count), Loc.T(L.History.TileCards), (record.CardsObtained.Count > 0) ? Styling.AccentNebula : Styling.TextMuted);
        var npcsX = cardsX - metricWidth;
        DrawMetric(npcsX, midY, metricWidth, Metric(record.NpcsCompleted), Loc.T(L.History.TileNpcs), (record.NpcsCompleted > 0) ? Styling.AccentAmber : Styling.TextMuted);
        var winsX = npcsX - metricWidth;
        DrawMetric(winsX, midY, metricWidth, Metric(record.MatchesWon), Loc.T(L.History.TileWins), (record.MatchesWon > 0) ? Styling.AccentMint : Styling.TextMuted);
        DrawMetric(winsX - metricWidth, midY, metricWidth, record.MatchesPlayed.ToString(Loc.Culture), Loc.T(L.History.TileMatches), Styling.AccentBlue);
        if (hit.Hovered)
        {
            Tooltip.Show(RunTooltip(record));
        }
    }

    private static float DrawModeIcon(TriadRunMode mode, float x, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var column = ModeIconColumn * globalScale;
        var icon = (mode == TriadRunMode.Farm) ? 61470 : 61449;
        var iconSize = TextDraw.IconSize((FontAwesomeIcon)icon);
        TextDraw.Icon((FontAwesomeIcon)icon, new Vector2(x + (column - iconSize.X) * 0.5f, midY - iconSize.Y * 0.5f), Styling.TextDim);
        return x + column + 10f * globalScale;
    }

    private static string Metric(int value)
    {
        if (value <= 0)
        {
            return NoValue;
        }
        return value.ToString("N0", Loc.Culture);
    }

    private static void DrawMetric(float x, float midY, float width, string value, string label, Vector4 color)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var valueSize = TextDraw.Measure(value);
        var labelSize = TextDraw.SmallCapsSize(label);
        var top = midY - (valueSize.Y + 2f * globalScale + labelSize.Y) * 0.5f;
        var centerX = x + width * 0.5f;
        TextDraw.At(value, new Vector2(centerX - valueSize.X * 0.5f, top), color);
        TextDraw.SmallCaps(label, new Vector2(centerX - labelSize.X * 0.5f, top + valueSize.Y + 2f * globalScale), Styling.TextMuted);
    }

    private static string ModeLabel(TriadRunMode mode)
    {
        return Loc.T((mode == TriadRunMode.Farm) ? L.Triad.ModeFarm : L.Triad.ModeCollect);
    }

    private static string RunTooltip(RunRecord record)
    {
        var tooltip = record.EndedAtUtc.ToLocalTime().ToString("g", Loc.Culture);
        if (record.MatchesPlayed > 0)
        {
            tooltip = tooltip + "\n" + Loc.T(L.History.TooltipRecord, record.MatchesWon, record.MatchesLost, record.MatchesDrawn);
        }
        if (record.NpcsSkipped > 0)
        {
            tooltip = tooltip + "\n" + Loc.Plural(L.History.TooltipSkipped, record.NpcsSkipped);
        }
        if (record.CardsObtained.Count == 0)
        {
            return tooltip;
        }
        var set = TriadData.Set;
        var cardNames = string.Empty;
        for (var i = 0; i < record.CardsObtained.Count; i++)
        {
            var cardName = set.CardName(record.CardsObtained[i]);
            cardNames = ((cardNames.Length == 0) ? cardName : (cardNames + ", " + cardName));
        }
        return tooltip + "\n" + Loc.T(L.History.TooltipCards, cardNames);
    }

    private static string RelativeTime(DateTime utc)
    {
        var age = DateTime.UtcNow - utc;
        if (age.TotalSeconds < 60.0)
        {
            return Loc.T(L.History.JustNow);
        }
        if (age.TotalMinutes < 60.0)
        {
            return Loc.T(L.History.MinutesAgo, (int)age.TotalMinutes);
        }
        if (age.TotalHours < 24.0)
        {
            return Loc.T(L.History.HoursAgo, (int)age.TotalHours);
        }
        if (!(age.TotalDays < 7.0))
        {
            return utc.ToLocalTime().ToString("M", Loc.Culture);
        }
        return Loc.T(L.History.DaysAgo, (int)age.TotalDays);
    }

    private void DrawClearControl(RunHistory history)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var fade = Motion.Transition(Motion.Key("##tta_hist_clear_state"), confirmClear);
        var slide = (1f - fade) * ConfirmSlide * globalScale;
        using (Motion.PushAlpha(fade))
        {
            if (!confirmClear)
            {
                var clearLabel = Loc.T(L.History.ClearHistory);
                ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + availWidth - PillButton.Width(clearLabel, FontAwesomeIcon.Trash) - slide, cursorScreenPos.Y));
                if (PillButton.Draw("##tta_hist_clear", clearLabel, Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Trash))
                {
                    confirmClear = true;
                }
                return;
            }
            var question = Loc.T(L.History.ClearQuestion);
            var yesLabel = Loc.T(L.History.ClearYes);
            var cancelLabel = Loc.T(L.Common.Cancel);
            var questionSize = TextDraw.Measure(question);
            var yesWidth = PillButton.Width(yesLabel);
            var cancelWidth = PillButton.Width(cancelLabel);
            var gap = 8f * globalScale;
            var rowHeight = 28f * globalScale;
            var buttonX = cursorScreenPos.X + availWidth - cancelWidth - slide;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, cursorScreenPos.Y));
            if (PillButton.Draw("##tta_hist_clear_no", cancelLabel, Styling.AccentGlow, PillButton.Emphasis.Ghost))
            {
                confirmClear = false;
            }
            buttonX -= gap + yesWidth;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, cursorScreenPos.Y));
            if (PillButton.Draw("##tta_hist_clear_yes", yesLabel, Styling.AccentRose, PillButton.Emphasis.Filled))
            {
                history.Clear();
                confirmClear = false;
            }
            TextDraw.At(question, new Vector2(buttonX - gap * 1.5f - questionSize.X, cursorScreenPos.Y + (rowHeight - questionSize.Y) * 0.5f), Styling.AccentRoseSoft);
        }
    }
}
