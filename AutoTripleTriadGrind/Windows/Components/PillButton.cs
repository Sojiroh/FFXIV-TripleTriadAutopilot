using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class PillButton
{
    public enum Emphasis
    {
        Ghost,
        Tinted,
        Filled
    }

    private const float PadX = 13f;

    private const float IconGap = 6f;

    private const float DefaultHeight = 28f;

    public static float Width(string label, FontAwesomeIcon? icon = null)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var iconWidth = icon is { } iconGlyph ? TextDraw.IconSize(iconGlyph).X + IconGap * globalScale : 0f;
        return PadX * 2f * globalScale + iconWidth + TextDraw.Measure(label).X;
    }

    public static bool Draw(string id, string label, Vector4 accent, Emphasis emphasis = Emphasis.Tinted, FontAwesomeIcon? icon = null, bool enabled = true, float height = DefaultHeight, string? tooltip = null)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(Width(label, icon), height * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var hit = Hit.Area(id, size, enabled);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var max = cursorScreenPos + size;
        var (fill, border, color) = Palette(accent, emphasis, hover, enabled);
        if (hit.Held)
        {
            fill = Styling.Darken(fill, 0.12f);
        }
        Paint.Pill(windowDrawList, cursorScreenPos, max, fill, border);
        if ((emphasis == Emphasis.Filled) & enabled)
        {
            Paint.TopLight(windowDrawList, cursorScreenPos, max, size.Y * 0.5f, 0.22f);
        }
        var midY = cursorScreenPos.Y + size.Y * 0.5f;
        var cursorX = cursorScreenPos.X + PadX * globalScale;
        if (icon is { } iconGlyph)
        {
            var iconSize = TextDraw.IconSize(iconGlyph);
            TextDraw.Icon(iconGlyph, new Vector2(cursorX, midY - iconSize.Y * 0.5f), color);
            cursorX += iconSize.X + IconGap * globalScale;
        }
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(cursorX, midY - labelSize.Y * 0.5f), color);
        if (hit.Hovered && tooltip != null)
        {
            Tooltip.Show(tooltip);
        }
        return hit.Clicked;
    }

    private static (Vector4 Fill, Vector4 Border, Vector4 Text) Palette(Vector4 accent, Emphasis emphasis, float hover, bool enabled)
    {
        if (!enabled)
        {
            return (Fill: Styling.WithAlpha(Styling.Surface1, 0.6f), Border: Styling.WithAlpha(Styling.BorderDim, 0.5f), Text: Styling.TextMuted);
        }
        switch (emphasis)
        {
            case Emphasis.Filled:
                return (Fill: Vector4.Lerp(accent, Styling.Lighten(accent, 0.15f), hover), Border: Styling.WithAlpha(Styling.Lighten(accent, 0.5f), 0.6f), Text: Styling.ForegroundOn(accent));
            case Emphasis.Tinted:
                return (Fill: Styling.WithAlpha(accent, 0.16f + 0.12f * hover), Border: Styling.WithAlpha(accent, 0.45f + 0.3f * hover), Text: Vector4.Lerp(Styling.Lighten(accent, 0.25f), Styling.TextStrong, hover * 0.5f));
            default:
                return (Fill: Styling.WithAlpha(Styling.Surface2, 0.9f * hover), Border: Styling.WithAlpha(Styling.BorderDim, 0.5f + 0.3f * hover), Text: Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover));
        }
    }
}
