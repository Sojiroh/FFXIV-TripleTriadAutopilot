using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Components;

internal sealed class SettingsGroup : IDisposable
{
    private const float PaddingX = 14f;

    private const float PaddingY = 8f;

    private const float GroupGap = 18f;

    private const float FootnotePullUp = 6f;

    private const float FootnoteIndent = 4f;

    internal static bool RowDrawnInGroup;

    private readonly Vector2 cardOrigin;

    private readonly float cardWidth;

    internal static float ContentRightEdge { get; private set; }

    public static SettingsGroup Begin(string title)
    {
        if (title.Length > 0)
        {
            Styling.SectionLabel(title);
            Styling.VSpace(6f);
        }
        return new SettingsGroup();
    }

    private SettingsGroup()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        cardOrigin = ImGui.GetCursorScreenPos();
        cardWidth = ImGui.GetContentRegionAvail().X;
        ContentRightEdge = cardOrigin.X + cardWidth - PaddingX * globalScale;
        RowDrawnInGroup = false;
        var windowDrawList = ImGui.GetWindowDrawList();
        windowDrawList.ChannelsSplit(2);
        windowDrawList.ChannelsSetCurrent(1);
        ImGui.SetCursorScreenPos(cardOrigin + new Vector2(PaddingX, PaddingY) * globalScale);
        ImGui.BeginGroup();
    }

    public void Dispose()
    {
        ImGui.EndGroup();
        var globalScale = ImGuiHelpers.GlobalScale;
        var max = new Vector2(cardOrigin.X + cardWidth, ImGui.GetItemRectMax().Y + PaddingY * globalScale);
        var windowDrawList = ImGui.GetWindowDrawList();
        windowDrawList.ChannelsSetCurrent(0);
        var rounding = Styling.CardRounding * globalScale;
        Paint.Surface(windowDrawList, cardOrigin, max, rounding, Styling.WithAlpha(Styling.Surface1, 0.55f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        windowDrawList.ChannelsMerge();
        ImGui.SetCursorScreenPos(new Vector2(cardOrigin.X, max.Y));
        ImGui.Dummy(new Vector2(cardWidth, 0f));
        Styling.VSpace(GroupGap);
    }

    public static void Footnote(string text)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - FootnotePullUp * globalScale);
        ImGui.Indent(FootnoteIndent * globalScale);
        using (Fonts.PushCaption())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.TextWrapped(text);
            }
        }
        ImGui.Unindent(FootnoteIndent * globalScale);
        Styling.VSpace(GroupGap);
    }

    public static float InnerRightLocalX() => ImGui.GetCursorPosX() + (ContentRightEdge - ImGui.GetCursorScreenPos().X);
}
