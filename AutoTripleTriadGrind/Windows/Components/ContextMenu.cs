using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class ContextMenu
{
    public readonly ref struct Scope
    {
        private readonly ImRaii.StyleDisposable style;

        private readonly ImRaii.ColorDisposable color;

        public bool Open { get; }

        public Scope(string id)
        {
            var globalScale = ImGuiHelpers.GlobalScale;
            style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(PaddingX, PaddingY) * globalScale).Push(ImGuiStyleVar.PopupRounding, 10f * globalScale).Push(ImGuiStyleVar.PopupBorderSize, 1f)
                .Push(ImGuiStyleVar.ItemSpacing, new Vector2(ItemPadX, ItemPadY) * globalScale)
                .Push(ImGuiStyleVar.FramePadding, new Vector2(ItemPadX, ItemPadY) * globalScale);
            color = ImRaii.PushColor(ImGuiCol.PopupBg, Styling.WithAlpha(Styling.Surface2, 0.98f)).Push(ImGuiCol.Border, Styling.WithAlpha(Styling.BorderDim, 0.85f)).Push(ImGuiCol.Text, Styling.TextSecondary)
                .Push(ImGuiCol.TextDisabled, Styling.TextMuted)
                .Push(ImGuiCol.HeaderHovered, Styling.WithAlpha(Styling.AccentGlow, 0.22f))
                .Push(ImGuiCol.Separator, Styling.WithAlpha(Styling.BorderDim, 0.6f));
            Open = ImGui.BeginPopup(id);
        }

        public void Dispose()
        {
            if (Open)
            {
                ImGui.EndPopup();
            }
            color.Dispose();
            style.Dispose();
        }
    }

    private const float PaddingX = 8f;

    private const float PaddingY = 8f;

    private const float ItemPadX = 10f;

    private const float ItemPadY = 6f;

    public static Scope Begin(string id) => new Scope(id);
}
