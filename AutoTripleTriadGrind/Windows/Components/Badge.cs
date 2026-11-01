using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class Badge
{
    private const float PadX = 7f;

    private const float PadY = 2f;

    public static float Draw(ImDrawListPtr drawList, string label, Vector4 color, float rightX, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var padX = PadX * globalScale;
            var padY = PadY * globalScale;
            var min = new Vector2(rightX - labelSize.X - padX * 2f, midY - labelSize.Y * 0.5f - padY);
            var max = new Vector2(rightX, midY + labelSize.Y * 0.5f + padY);
            Paint.Pill(drawList, min, max, Styling.WithAlpha(color, 0.16f), Styling.WithAlpha(color, 0.5f));
            TextDraw.At(label, new Vector2(min.X + padX, midY - labelSize.Y * 0.5f), Styling.Lighten(color, 0.25f));
            return max.X - min.X;
        }
    }
}
