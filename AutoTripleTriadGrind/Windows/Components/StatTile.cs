using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class StatTile
{
    private const float PadX = 13f;

    private const float PadY = 10f;

    public static void Draw(string label, string value, string? sub, Vector4 accent, float width, float height = Layout.StatTileHeight)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, height * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Glass(windowDrawList, cursorScreenPos, max, Styling.CardRounding * globalScale, accent, 0.05f);
        var padX = PadX * globalScale;
        var padY = PadY * globalScale;
        var labelSize = TextDraw.SmallCapsSize(label);
        var dotRadius = 3f * globalScale;
        windowDrawList.AddCircleFilled(new Vector2(cursorScreenPos.X + padX + dotRadius, cursorScreenPos.Y + padY + labelSize.Y * 0.5f), dotRadius, Paint.Col(accent));
        TextDraw.SmallCaps(label, new Vector2(cursorScreenPos.X + padX + dotRadius * 2f + 6f * globalScale, cursorScreenPos.Y + padY), Styling.TextDim);
        var subReserve = 0f;
        if (!string.IsNullOrEmpty(sub))
        {
            using (Fonts.PushCaption())
            {
                var subSize = TextDraw.Measure(sub);
                subReserve = subSize.X + 8f * globalScale;
                TextDraw.At(sub, new Vector2(max.X - padX - subSize.X, max.Y - padY - subSize.Y - 1f * globalScale), Styling.TextDim);
            }
        }
        using (Fonts.PushHeadline())
        {
            var displayValue = TextDraw.Truncate(value, size.X - padX * 2f - subReserve);
            var valueSize = TextDraw.Measure(displayValue);
            TextDraw.At(displayValue, new Vector2(cursorScreenPos.X + padX, max.Y - padY - valueSize.Y), Styling.TextStrong);
        }
        ImGui.Dummy(size);
    }
}
