using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class FilterChip
{
    private const float PadX = 11f;

    private const float DotRadius = 3.5f;

    private const float Gap = 7f;

    public static float Width(string label, string? count, FontAwesomeIcon? trailingIcon = null)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var width = PadX * 2f * globalScale + DotRadius * 2f * globalScale + Gap * globalScale + TextDraw.Measure(label).X;
        if (count != null)
        {
            width += Gap * globalScale + TextDraw.Measure(count).X;
        }
        if (trailingIcon.HasValue)
        {
            var icon = trailingIcon.GetValueOrDefault();
            width += Gap * globalScale + TextDraw.IconSize(icon).X;
        }
        return width;
    }

    public static bool Draw(string id, string label, string? count, Vector4 accent, bool active, float height, FontAwesomeIcon? trailingIcon = null, string? tooltip = null)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(Width(label, count, trailingIcon), height);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        var hit = Hit.Area(id, size);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var amount = Motion.Approach(Motion.Key(id, 1), active ? 1f : 0f, 18f);
        var windowDrawList = ImGui.GetWindowDrawList();
        var fill = Vector4.Lerp(Styling.WithAlpha(Styling.Surface1, 0.5f + 0.4f * hover), Styling.WithAlpha(accent, 0.16f + 0.08f * hover), amount);
        var border = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.45f + 0.3f * hover), Styling.WithAlpha(accent, 0.5f + 0.25f * hover), amount);
        Paint.Pill(windowDrawList, cursorScreenPos, max, fill, border);
        var midY = cursorScreenPos.Y + size.Y * 0.5f;
        var cursorX = cursorScreenPos.X + PadX * globalScale;
        var dotRadius = DotRadius * globalScale;
        var center = new Vector2(cursorX + dotRadius, midY);
        if (active)
        {
            windowDrawList.AddCircleFilled(center, dotRadius, Paint.Col(accent));
        }
        else
        {
            windowDrawList.AddCircle(center, dotRadius, Paint.Col(Styling.WithAlpha(accent, 0.7f)), 0, 1.2f * globalScale);
        }
        cursorX += dotRadius * 2f + Gap * globalScale;
        var color = Vector4.Lerp(Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover), Styling.TextStrong, amount);
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(cursorX, midY - labelSize.Y * 0.5f), color);
        cursorX += labelSize.X;
        if (count != null)
        {
            cursorX += Gap * globalScale;
            var countSize = TextDraw.Measure(count);
            TextDraw.At(count, new Vector2(cursorX, midY - countSize.Y * 0.5f), Vector4.Lerp(Styling.TextMuted, Styling.Lighten(accent, 0.2f), amount));
            cursorX += countSize.X;
        }
        if (trailingIcon is { } icon)
        {
            cursorX += Gap * globalScale;
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(cursorX, midY - iconSize.Y * 0.5f), Vector4.Lerp(Styling.TextDim, Styling.TextStrong, hover));
        }
        if (hit.Hovered && tooltip != null)
        {
            Tooltip.Show(tooltip);
        }
        return hit.Clicked;
    }
}
