using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Windows.Components;
using AutoTripleTriadGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Shell;

internal static class MiniPlayer
{
    private const float PadX = 18f;

    private const float ButtonSize = 34f;

    private const float ButtonGap = 8f;

    private const float BarWidth = 160f;

    private const float BarHeight = 8f;

    public static bool Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var end = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        var controller = plugin.Controller;
        var info = ReadyState.Resolve(plugin.Configuration, controller);
        var workload = RunWorkload.Measure(controller);
        Dock.Background(windowDrawList, cursorScreenPos, end, windowRounding);
        var padX = PadX * globalScale;
        var buttonSize = ButtonSize * globalScale;
        var buttonsWidth = buttonSize * 2f + ButtonGap * globalScale;
        ImGui.SetCursorScreenPos(cursorScreenPos);
        var openHit = Hit.Area("##tta_mini_open", new Vector2(size.X - padX - buttonsWidth - ButtonGap * globalScale, size.Y));
        var hover = Motion.Hover(Motion.Key("##tta_mini_open"), openHit.Hovered);
        if (hover > 0.01f)
        {
            Paint.Fill(windowDrawList, cursorScreenPos, end, Styling.WithAlpha(Styling.Surface2, 0.35f * hover), windowRounding, ImDrawFlags.RoundCornersBottom);
        }
        var centerY = cursorScreenPos.Y + size.Y * 0.5f;
        var color = (controller.Paused ? info.Accent : Styling.PulseColor(info.Accent, info.AccentSoft));
        Paint.Dot(windowDrawList, new Vector2(cursorScreenPos.X + padX + 4f * globalScale, centerY), 4f * globalScale, color);
        var barWidth = BarWidth * globalScale;
        var barLeft = end.X - padX - buttonsWidth - 16f * globalScale - barWidth;
        var origin = new Vector2(barLeft, centerY - BarHeight * globalScale * 0.5f);
        if (workload.Needed > 0)
        {
            Paint.Bar(windowDrawList, origin, barWidth, BarHeight * globalScale, workload.Fraction, info.Accent);
        }
        else if (controller.Paused)
        {
            Paint.Bar(windowDrawList, origin, barWidth, BarHeight * globalScale, 0f, info.Accent);
        }
        else
        {
            Paint.IndeterminateBar(windowDrawList, origin, barWidth, BarHeight * globalScale, info.Accent);
        }
        var textX = cursorScreenPos.X + padX + 22f * globalScale;
        var label = (controller.Paused ? Loc.T(L.Run.PhasePaused) : ReadyState.PhaseLabel(controller.Phase));
        var labelSize = TextDraw.SmallCapsSize(label);
        var textLineHeight = ImGui.GetTextLineHeight();
        var lineGap = 2f * globalScale;
        var textY = centerY - (labelSize.Y + lineGap + textLineHeight) * 0.5f;
        TextDraw.SmallCaps(label, new Vector2(textX, textY), info.AccentSoft);
        TextDraw.At(TextDraw.Truncate(controller.Status, barLeft - 16f * globalScale - textX), new Vector2(textX, textY + labelSize.Y + lineGap), Styling.TextStrong);
        var resumeBlocked = controller.Paused && controller.PauseReason == PauseReason.InContent;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize * 2f - ButtonGap * globalScale, centerY - buttonSize * 0.5f));
        var icon = (controller.Paused ? 61515 : 61516);
        if (IconButton.Draw((FontAwesomeIcon)icon, "##tta_mini_pause", buttonSize, controller.Paused ? Styling.AccentMintSoft : Styling.AccentAmberSoft, resumeBlocked ? Loc.T(L.Shell.ResumeBlocked) : (controller.Paused ? Loc.T(L.Common.Resume) : Loc.T(L.Common.Pause)), !resumeBlocked))
        {
            controller.TogglePause();
        }
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, centerY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Stop, "##tta_mini_stop", buttonSize, Styling.AccentRose, Loc.T(L.Common.StopRun)))
        {
            controller.Stop();
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
        return openHit.Clicked;
    }
}
