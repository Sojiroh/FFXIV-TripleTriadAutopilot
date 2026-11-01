using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using AutoTripleTriadGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Shell;

internal static class HeaderBar
{
    private const string Title = "Triple Triad Autopilot";

    private const float PadX = 16f;

    private const float IconBox = 26f;

    private const float ButtonSize = 30f;

    private const float ButtonGap = 6f;

    private const int ButtonCount = 2;

    private const float CompactBarWidth = 90f;

    public const float MinimumWidth = 136f;

    public static float ButtonsWidth()
    {
        return (ButtonCount * ButtonSize + ButtonGap) * ImGuiHelpers.GlobalScale;
    }

    public static bool HandleDrag(Vector2 windowPos, float width, float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var dragWidth = width - PadX * globalScale - ButtonsWidth() - 8f * globalScale;
        ImGui.SetCursorScreenPos(windowPos);
        ImGui.InvisibleButton("##tta_drag", new Vector2(MathF.Max(1f, dragWidth), height));
        var doubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
        if (!ImGui.IsItemActive())
        {
            return doubleClicked;
        }
        var mouseDelta = ImGui.GetIO().MouseDelta;
        if (mouseDelta != Vector2.Zero)
        {
            ImGui.SetWindowPos(ImGui.GetWindowPos() + mouseDelta, ImGuiCond.Always);
        }
        return doubleClicked;
    }

    public static void Draw(AppWindow window, Plugin plugin, Vector2 origin, float width, float height, float windowRounding, bool compact)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var end = origin + new Vector2(width, height);
        var padX = PadX * globalScale;
        var midY = origin.Y + height * 0.5f;
        Paint.Fill(windowDrawList, origin, end, Styling.WithAlpha(Styling.Surface1, 0.4f), windowRounding, compact ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop);
        if (!compact)
        {
            Paint.Hairline(windowDrawList, new Vector2(origin.X, end.Y - 0.5f), new Vector2(end.X, end.Y - 0.5f));
        }
        var iconSize = IconBox * globalScale;
        var iconMin = new Vector2(origin.X + padX, midY - iconSize * 0.5f);
        AppIcon.Draw(windowDrawList, iconMin, iconMin + new Vector2(iconSize, iconSize), 7f * globalScale);
        var rightLimit = end.X - padX - ButtonsWidth();
        var cursorX = iconMin.X + iconSize + 12f * globalScale;
        using (Fonts.PushHeadline())
        {
            var titleSize = TextDraw.Measure(Title);
            if (cursorX + titleSize.X <= rightLimit)
            {
                TextDraw.At(Title, new Vector2(cursorX, midY - titleSize.Y * 0.5f), Styling.TextStrong);
                cursorX += titleSize.X + 14f * globalScale;
            }
        }
        var info = ReadyState.Resolve(plugin.Configuration, plugin.Controller);
        var pillRight = DrawStatusPill(windowDrawList, info, cursorX, rightLimit, midY);
        if (pillRight > cursorX)
        {
            cursorX = pillRight + 14f * globalScale;
        }
        if (compact)
        {
            DrawCompactInfo(plugin, info, cursorX, rightLimit - 14f * globalScale, midY);
        }
        DrawButtons(window, end, midY, compact);
    }

    private static void DrawButtons(AppWindow window, Vector2 end, float midY, bool compact)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var padX = PadX * globalScale;
        var buttonSize = ButtonSize * globalScale;
        var step = buttonSize + ButtonGap * globalScale;
        var buttonY = midY - buttonSize * 0.5f;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, buttonY));
        if (IconButton.Draw(FontAwesomeIcon.Times, "##tta_close", buttonSize, tooltip: Loc.T(L.Common.Close)))
        {
            window.IsOpen = false;
        }
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize - step, buttonY));
        var icon = (compact ? 61559 : 61560);
        if (IconButton.Draw((FontAwesomeIcon)icon, "##tta_minimize", buttonSize, tooltip: compact ? Loc.T(L.Shell.Restore) : Loc.T(L.Shell.Minimize)))
        {
            window.ToggleCompact();
        }
    }

    private static float DrawStatusPill(ImDrawListPtr drawList, ReadyState.Info info, float x, float rightLimit, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var label = ReadyState.ShortLabel(info.Kind);
        var padX = 10f * globalScale;
        var dotRadius = 3.5f * globalScale;
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var pillHeight = labelSize.Y + 8f * globalScale;
            var pillMin = new Vector2(x, midY - pillHeight * 0.5f);
            var pillMax = pillMin + new Vector2(padX * 2f + dotRadius * 2f + 6f * globalScale + labelSize.X, pillHeight);
            if (pillMax.X > rightLimit)
            {
                return x;
            }
            Paint.Pill(drawList, pillMin, pillMax, Styling.WithAlpha(info.Accent, 0.16f), Styling.WithAlpha(info.Accent, 0.45f));
            var color = ((info.Kind == ReadyState.Kind.Running) ? Styling.PulseColor(info.Accent, info.AccentSoft) : info.Accent);
            drawList.AddCircleFilled(new Vector2(pillMin.X + padX + dotRadius, midY), dotRadius, Paint.Col(color));
            TextDraw.At(label, new Vector2(pillMin.X + padX + dotRadius * 2f + 6f * globalScale, midY - labelSize.Y * 0.5f), info.AccentSoft);
            return pillMax.X;
        }
    }

    private static void DrawCompactInfo(Plugin plugin, ReadyState.Info info, float x, float rightX, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var controller = plugin.Controller;
        if (rightX - x < CompactBarWidth * globalScale)
        {
            return;
        }
        if (!controller.Running)
        {
            var assessment = TriadLauncher.Assess(plugin.Configuration);
            if (assessment.Readiness != TriadLauncher.Readiness.Ready)
            {
                return;
            }
            var sublabel = TriadLauncher.Sublabel(in assessment);
            using (Fonts.PushCaption())
            {
                var sublabelSize = TextDraw.Measure(sublabel);
                TextDraw.At(TextDraw.Truncate(sublabel, rightX - x), new Vector2(x, midY - sublabelSize.Y * 0.5f), Styling.TextDim);
            }
            return;
        }
        var workload = RunWorkload.Measure(controller);
        var windowDrawList = ImGui.GetWindowDrawList();
        var barWidth = CompactBarWidth * globalScale;
        var barLeft = rightX - barWidth;
        var barHeight = 6f * globalScale;
        var origin = new Vector2(barLeft, midY - barHeight * 0.5f);
        if (workload.Needed > 0)
        {
            Paint.Bar(windowDrawList, origin, barWidth, barHeight, workload.Fraction, info.Accent);
        }
        else if (controller.Paused)
        {
            Paint.Bar(windowDrawList, origin, barWidth, barHeight, 0f, info.Accent);
        }
        else
        {
            Paint.IndeterminateBar(windowDrawList, origin, barWidth, barHeight, info.Accent);
        }
        var textWidth = barLeft - 12f * globalScale - x;
        if (textWidth <= 0f)
        {
            return;
        }
        var phaseLabel = (controller.Paused ? Loc.T(L.Run.PhasePaused) : ReadyState.PhaseLabel(controller.Phase));
        var status = controller.Status;
        using (Fonts.PushCaption())
        {
            var truncatedPhase = TextDraw.Truncate(phaseLabel, textWidth);
            var phaseSize = TextDraw.Measure(truncatedPhase);
            var textY = midY - phaseSize.Y * 0.5f;
            TextDraw.At(truncatedPhase, new Vector2(x, textY), Styling.TextSecondary);
            TextDraw.Trailing(status, x + phaseSize.X, x + textWidth, textY, Styling.TextSecondary);
        }
    }
}
