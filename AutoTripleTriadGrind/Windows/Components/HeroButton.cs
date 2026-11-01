using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class HeroButton
{
    private const float Rounding = 14f;

    private const float PadX = 20f;

    public static bool Draw(FontAwesomeIcon icon, string title, string? sublabel, Vector4 accent, bool enabled, string? disabledReason = null, float width = 0f)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = Layout.HeroButtonHeight * globalScale;
        if (width <= 0f)
        {
            width = ImGui.GetContentRegionAvail().X;
        }
        var size = new Vector2(width, height);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        ImGui.PushID((nint)icon);
        var hit = Hit.Area("##hero", size, enabled);
        var hover = Motion.Hover(Motion.Key("##hero"), hit.Hovered);
        var press = Motion.Approach(Motion.Key("##hero", 1), hit.Held ? 1f : 0f, 30f);
        ImGui.PopID();
        var y = (enabled ? ((hover * 2f - press * 2f) * globalScale) : 0f);
        var min = cursorScreenPos - new Vector2(0f, y);
        var max = min + size;
        var rounding = Rounding * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        if (enabled)
        {
            var pulse = 0.5f + 0.5f * Styling.Pulse(Styling.PulseBreath);
            Paint.Glow(windowDrawList, min, max, rounding, accent, 0.35f + 0.35f * pulse + hover * 0.6f);
            Paint.Shadow(windowDrawList, min, max, rounding, 8f * globalScale, 0.5f);
            var top = Vector4.Lerp(Styling.Lighten(accent, 0.18f), Styling.Lighten(accent, 0.3f), hover);
            var bottom = Vector4.Lerp(Styling.Darken(accent, 0.14f), accent, hover);
            Paint.Gradient(windowDrawList, min, max, top, bottom, rounding);
            Paint.TopLight(windowDrawList, min, max, rounding, 0.28f);
            Paint.Stroke(windowDrawList, min, max, Styling.WithAlpha(Styling.Lighten(accent, 0.5f), 0.55f + hover * 0.3f), rounding);
        }
        else
        {
            Paint.Fill(windowDrawList, min, max, Styling.WithAlpha(Styling.Surface1, 0.8f), rounding);
            Paint.Stroke(windowDrawList, min, max, Styling.WithAlpha(Styling.BorderDim, 0.7f), rounding);
        }
        var padX = PadX * globalScale;
        var midY = min.Y + height * 0.5f;
        var displayIcon = (enabled ? icon : FontAwesomeIcon.Lock);
        var foreground = (enabled ? Styling.ForegroundOn(accent) : Styling.TextMuted);
        var iconSize = TextDraw.IconSize(displayIcon);
        TextDraw.Icon(displayIcon, new Vector2(min.X + padX, midY - iconSize.Y * 0.5f), foreground);
        using (Fonts.PushHeadline())
        {
            var titleSize = TextDraw.Measure(title);
            TextDraw.At(title, new Vector2(min.X + padX + iconSize.X + 12f * globalScale, midY - titleSize.Y * 0.5f), foreground);
        }
        var subtext = (enabled ? sublabel : (disabledReason ?? sublabel));
        if (!string.IsNullOrEmpty(subtext))
        {
            var subtextSize = TextDraw.Measure(subtext);
            TextDraw.At(subtext, new Vector2(max.X - padX - subtextSize.X, midY - subtextSize.Y * 0.5f), enabled ? Styling.WithAlpha(foreground, 0.8f) : Styling.TextDim);
        }
        return hit.Clicked;
    }
}
