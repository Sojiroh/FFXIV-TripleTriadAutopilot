using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class SidebarTab
{
    private const float Height = 40f;

    private const float PadX = 14f;

    private const float IconGap = 11f;

    public static bool Draw(string label, FontAwesomeIcon icon, Vector4 accent, bool selected)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Height * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        var hit = Hit.Area(label, size);
        var hover = Motion.Hover(Motion.Key(label), hit.Hovered);
        var selection = Motion.Approach(Motion.Key(label, 1), selected ? 1f : 0f, 16f);
        var windowDrawList = ImGui.GetWindowDrawList();
        var rounding = 9f * globalScale;
        var backgroundAlpha = MathF.Max(hover * 0.7f, selection) * 0.9f;
        if (backgroundAlpha > 0.01f)
        {
            Paint.Fill(windowDrawList, cursorScreenPos, max, Styling.WithAlpha(Styling.Tint(Styling.Surface2, accent, selection * 0.18f), backgroundAlpha), rounding);
        }
        if (selection > 0.01f)
        {
            var min = new Vector2(cursorScreenPos.X, cursorScreenPos.Y + size.Y * (0.5f - 0.25f * selection));
            var barMax = new Vector2(cursorScreenPos.X + 3f * globalScale, cursorScreenPos.Y + size.Y * (0.5f + 0.25f * selection));
            Paint.Fill(windowDrawList, min, barMax, Styling.WithAlpha(accent, selection), 2f * globalScale);
        }
        var iconColor = Vector4.Lerp(Styling.TextDim, accent, selection);
        var labelColor = Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(hover, selection));
        var midY = cursorScreenPos.Y + size.Y * 0.5f;
        var iconSize = TextDraw.IconSize(icon);
        TextDraw.Icon(icon, new Vector2(cursorScreenPos.X + PadX * globalScale, midY - iconSize.Y * 0.5f), iconColor);
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(cursorScreenPos.X + PadX * globalScale + iconSize.X + IconGap * globalScale, midY - labelSize.Y * 0.5f), labelColor);
        return hit.Clicked;
    }
}
