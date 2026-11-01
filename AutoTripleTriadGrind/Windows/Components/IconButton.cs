using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class IconButton
{
    public static bool Draw(FontAwesomeIcon icon, string id, float size, Vector4? color = null, string? tooltip = null, bool enabled = true)
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var bounds = new Vector2(size, size);
        var result = Hit.Area(id, bounds, enabled);
        var hover = Motion.Hover(Motion.Key(id), result.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var center = cursorScreenPos + bounds * 0.5f;
        if (hover > 0.01f)
        {
            var a = hover * (result.Held ? 1f : 0.85f);
            windowDrawList.AddCircleFilled(center, size * 0.5f, Paint.Col(Styling.WithAlpha(Styling.Surface3, a)));
        }
        var baseColor = color ?? Styling.TextSecondary;
        var iconColor = (enabled ? Vector4.Lerp(baseColor, Styling.TextStrong, hover * 0.55f) : Styling.TextMuted);
        TextDraw.IconCentered(icon, center, iconColor);
        if (result.Hovered && tooltip != null)
        {
            Tooltip.Show(tooltip);
        }
        return result.Clicked;
    }
}
