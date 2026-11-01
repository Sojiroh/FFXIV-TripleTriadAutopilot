using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class ToggleSwitch
{
    private const float TrackWidth = 40f;

    private const float TrackHeight = 22f;

    private const float KnobInset = 3f;

    public static bool Draw(string id, ref bool value)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(TrackWidth * globalScale, TrackHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var hit = Hit.Area(id, size);
        var changed = false;
        if (hit.Clicked)
        {
            value = !value;
            changed = true;
        }
        var checkedAmount = Motion.Approach(Motion.Key(id), value ? 1f : 0f, 16f);
        var hover = Motion.Hover(Motion.Key(id, 1), hit.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var max = cursorScreenPos + size;
        var rounding = size.Y * 0.5f;
        var offColor = Vector4.Lerp(Styling.Surface2, Styling.Surface3, hover);
        var onColor = Vector4.Lerp(Styling.AccentGlow, Styling.AccentGlowSoft, hover * 0.4f);
        var trackColor = Vector4.Lerp(offColor, onColor, checkedAmount);
        if (checkedAmount > 0.01f)
        {
            var glowPad = new Vector2(2f * globalScale, 2f * globalScale);
            windowDrawList.AddRectFilled(cursorScreenPos - glowPad, max + glowPad, Paint.Col(Styling.WithAlpha(Styling.AccentGlow, 0.18f * checkedAmount)), rounding + glowPad.X);
        }
        Paint.Fill(windowDrawList, cursorScreenPos, max, trackColor, rounding);
        var borderColor = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.8f), Styling.WithAlpha(Styling.AccentGlowSoft, 0.6f), checkedAmount);
        Paint.Stroke(windowDrawList, cursorScreenPos, max, borderColor, rounding);
        var knobInset = KnobInset * globalScale;
        var knobRadius = (size.Y - knobInset * 2f) * 0.5f;
        var knobTravel = size.X - (knobInset + knobRadius) * 2f;
        var knobCenter = new Vector2(cursorScreenPos.X + knobInset + knobRadius + knobTravel * checkedAmount, cursorScreenPos.Y + size.Y * 0.5f);
        windowDrawList.AddCircleFilled(knobCenter + new Vector2(0f, 1f * globalScale), knobRadius, Paint.Col(new Vector4(0f, 0f, 0f, 0.3f)));
        windowDrawList.AddCircleFilled(knobCenter, knobRadius, Paint.Col(Vector4.Lerp(Styling.TextSecondary, Styling.ForegroundOn(onColor), checkedAmount)));
        return changed;
    }
}
