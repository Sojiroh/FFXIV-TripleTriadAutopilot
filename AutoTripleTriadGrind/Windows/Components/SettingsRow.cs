using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class SettingsRow
{
    private readonly record struct RowArea(Vector2 Origin, float RightEdge, float MiddleY, bool Hovered)
    {
        public float Width => RightEdge - Origin.X;
    }

    private const float RowHeight = 40f;

    private const float HelpIconGap = 7f;

    private const float CaptionPullUp = 7f;

    private const float CaptionBottomGap = 10f;

    private const float BlockBottomGap = 6f;

    private const float NoteTopGap = 6f;

    private const float NoteBottomGap = 8f;

    public const float ToggleHeight = 22f;

    public static void Draw(string label, string? help, float controlWidth, Action drawControl, float controlHeight = 0f)
    {
        var area = BeginRow();
        DrawTopDivider(area);
        var labelHovered = DrawLabel(area, label);
        var helpHovered = DrawHelpIcon(area, label, help);
        if (!string.IsNullOrEmpty(help) && (labelHovered | helpHovered))
        {
            Tooltip.Show(help);
        }
        DrawControl(area, controlWidth, controlHeight, drawControl);
        EndRow(area);
    }

    public static void DrawBlock(string label, string? help, Action drawContent)
    {
        Draw(label, help, 0f, () =>
        {
        });
        drawContent();
        Styling.VSpace(BlockBottomGap);
    }

    public static void Caption(string text)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(cursorScreenPos with { Y = cursorScreenPos.Y - CaptionPullUp * globalScale });
        var wrapLocalPosX = ImGui.GetCursorPosX() + (SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X);
        using (Fonts.PushCaption())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.PushTextWrapPos(wrapLocalPosX);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
            }
        }
        Styling.VSpace(CaptionBottomGap);
    }

    public static void Note(string text, Vector4? color = null)
    {
        Styling.VSpace(NoteTopGap);
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + (SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X));
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? Styling.TextMuted))
        {
            ImGui.TextUnformatted(text);
        }
        ImGui.PopTextWrapPos();
        Styling.VSpace(NoteBottomGap);
    }

    private static RowArea BeginRow()
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var contentRightEdge = SettingsGroup.ContentRightEdge;
        var height = RowHeight * ImGuiHelpers.GlobalScale;
        var hovered = ImGui.IsMouseHoveringRect(cursorScreenPos, cursorScreenPos + new Vector2(contentRightEdge - cursorScreenPos.X, height));
        return new RowArea(cursorScreenPos, contentRightEdge, cursorScreenPos.Y + height * 0.5f, hovered);
    }

    private static void DrawTopDivider(RowArea area)
    {
        if (SettingsGroup.RowDrawnInGroup)
        {
            var windowDrawList = ImGui.GetWindowDrawList();
            windowDrawList.AddLine(area.Origin, area.Origin with { X = area.RightEdge }, ImGui.GetColorU32(Styling.Hairline), 1f);
        }
        SettingsGroup.RowDrawnInGroup = true;
    }

    private static bool DrawLabel(RowArea area, string label)
    {
        var labelSize = ImGui.CalcTextSize(label);
        ImGui.SetCursorScreenPos(new Vector2(area.Origin.X, area.MiddleY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, area.Hovered ? Styling.TextStrong : Styling.TextSecondary))
        {
            ImGui.TextUnformatted(label);
        }
        return ImGui.IsItemHovered();
    }

    private static bool DrawHelpIcon(RowArea area, string label, string? help)
    {
        if (string.IsNullOrEmpty(help) || !area.Hovered)
        {
            return false;
        }
        var labelWidth = ImGui.CalcTextSize(label).X;
        var glyph = FontAwesomeIcon.InfoCircle.ToIconString();
        using (Fonts.PushIcon())
        {
            var glyphSize = ImGui.CalcTextSize(glyph);
            ImGui.SetCursorScreenPos(new Vector2(area.Origin.X + labelWidth + HelpIconGap * ImGuiHelpers.GlobalScale, area.MiddleY - glyphSize.Y * 0.5f));
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.WithAlpha(Styling.TextMuted, 0.9f)))
            {
                ImGui.TextUnformatted(glyph);
            }
        }
        return ImGui.IsItemHovered();
    }

    private static void DrawControl(RowArea area, float controlWidth, float controlHeight, Action drawControl)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = ((controlHeight > 0f) ? (controlHeight * globalScale) : ImGui.GetFrameHeight());
        ImGui.SetCursorScreenPos(new Vector2(area.RightEdge - controlWidth * globalScale, area.MiddleY - height * 0.5f));
        drawControl();
    }

    private static void EndRow(RowArea area)
    {
        ImGui.SetCursorScreenPos(area.Origin);
        ImGui.Dummy(new Vector2(area.Width, RowHeight * ImGuiHelpers.GlobalScale));
    }
}
