using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class Tooltip
{
    private sealed class Scope : IDisposable
    {
        private readonly IDisposable font;

        private readonly IDisposable color;

        private readonly IDisposable style;

        public Scope(IDisposable font, IDisposable color, IDisposable style)
        {
            this.font = font;
            this.color = color;
            this.style = style;
            ImGui.BeginTooltip();
        }

        public void Dispose()
        {
            ImGui.EndTooltip();
            font.Dispose();
            color.Dispose();
            style.Dispose();
        }
    }

    private const float PaddingX = 14f;

    private const float PaddingY = 11f;

    private const float WrapWidth = 320f;

    private const float LineGap = 5f;

    public static void Show(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        using (Begin())
        {
            Text(text);
        }
    }

    public static IDisposable Begin()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(PaddingX, PaddingY) * globalScale).Push(ImGuiStyleVar.WindowRounding, 10f * globalScale).Push(ImGuiStyleVar.PopupRounding, 10f * globalScale)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f)
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, LineGap) * globalScale);
        var color = ImRaii.PushColor(ImGuiCol.PopupBg, Styling.WithAlpha(Styling.Surface2, 0.98f)).Push(ImGuiCol.Border, Styling.WithAlpha(Styling.BorderDim, 0.85f)).Push(ImGuiCol.Text, Styling.TextSecondary);
        return new Scope(Fonts.PushBody(), color, style);
    }

    public static void Text(string text, Vector4? color = null)
    {
        ImGui.PushTextWrapPos(WrapWidth * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? Styling.TextSecondary))
        {
            ImGui.TextUnformatted(text);
        }
        ImGui.PopTextWrapPos();
    }
}
