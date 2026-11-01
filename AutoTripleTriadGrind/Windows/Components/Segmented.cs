using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class Segmented
{
    public readonly record struct Item(FontAwesomeIcon? Icon, string Label);

    private readonly record struct Metrics(Vector2 Icon, Vector2 Label, float Content);

    public const int MaxItems = 8;

    private const float Inset = 3f;

    private const float SegmentPadX = 14f;

    private const float IconGap = 8f;

    private static readonly Metrics[] metrics = new Metrics[MaxItems];

    private static readonly float[] segmentWidths = new float[MaxItems];

    public static float PreferredWidth(ReadOnlySpan<Item> items)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var count = Math.Min(items.Length, MaxItems);
        var maxContent = 0f;
        for (int i = 0; i < count; i++)
        {
            maxContent = MathF.Max(maxContent, Measure(items[i]).Content);
        }
        return (maxContent + SegmentPadX * 2f * globalScale) * (float)count + Inset * 2f * globalScale;
    }

    public static bool Draw(string id, ReadOnlySpan<Item> items, ref int selected, bool enabled = true, float height = Layout.SegmentHeight, float width = 0f)
    {
        var count = Math.Min(items.Length, MaxItems);
        if (count == 0)
        {
            return false;
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2((width > 0f) ? width : ImGui.GetContentRegionAvail().X, height * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + size;
        var inset = Inset * globalScale;
        var rounding = size.Y * 0.5f;
        var windowDrawList = ImGui.GetWindowDrawList();
        var selectedIndex = Math.Clamp(selected, 0, count - 1);
        ResolveWidths(items, count, size.X - inset * 2f);
        Paint.Fill(windowDrawList, cursorScreenPos, max, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Paint.Stroke(windowDrawList, cursorScreenPos, max, Styling.WithAlpha(Styling.BorderDim, 0.6f), rounding);
        ImGui.PushID(id);
        var highlightX = Motion.Approach(Motion.Key("##seg"), SegmentOffset(selectedIndex), 18f);
        var highlightWidth = Motion.Approach(Motion.Key("##seg", 1), segmentWidths[selectedIndex], 18f);
        var highlightMin = new Vector2(cursorScreenPos.X + inset + highlightX, cursorScreenPos.Y + inset);
        var highlightMax = highlightMin + new Vector2(highlightWidth, size.Y - inset * 2f);
        var accent = (enabled ? Styling.AccentGlow : Styling.Surface3);
        Paint.Gradient(windowDrawList, highlightMin, highlightMax, Styling.Tint(Styling.Surface3, accent, 0.3f), Styling.Tint(Styling.Surface2, accent, 0.22f), rounding - inset);
        Paint.TopLight(windowDrawList, highlightMin, highlightMax, rounding - inset, 0.1f);
        Paint.Stroke(windowDrawList, highlightMin, highlightMax, Styling.WithAlpha(enabled ? Styling.AccentGlowSoft : Styling.BorderDim, 0.55f), rounding - inset);
        var result = false;
        var segmentX = cursorScreenPos.X + inset;
        for (int i = 0; i < count; i++)
        {
            var segmentWidth = segmentWidths[i];
            var segmentMin = new Vector2(segmentX, cursorScreenPos.Y);
            ImGui.SetCursorScreenPos(segmentMin);
            ImGui.PushID(i + 1);
            var hit = Hit.Area("##segment", new Vector2(segmentWidth, size.Y), enabled);
            var hover = Motion.Hover(Motion.Key("##segment"), hit.Hovered);
            ImGui.PopID();
            if (hit.Clicked && selected != i)
            {
                selected = i;
                result = true;
            }
            var isSelected = i == selectedIndex;
            if (!isSelected && hover > 0.01f)
            {
                Paint.Fill(windowDrawList, segmentMin + new Vector2(0f, inset), segmentMin + new Vector2(segmentWidth, size.Y - inset), Styling.WithAlpha(Styling.Surface2, 0.7f * hover), rounding - inset);
            }
            DrawContent(items[i], metrics[i], segmentMin, segmentWidth, size.Y, isSelected, hover, enabled);
            segmentX += segmentWidth;
        }
        ImGui.PopID();
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
        return result;
    }

    private static void ResolveWidths(ReadOnlySpan<Item> items, int count, float inner)
    {
        var padding = SegmentPadX * 2f * ImGuiHelpers.GlobalScale;
        var equalWidth = inner / (float)count;
        var maxContent = 0f;
        var totalContent = 0f;
        for (int i = 0; i < count; i++)
        {
            metrics[i] = Measure(items[i]);
            maxContent = MathF.Max(maxContent, metrics[i].Content);
            totalContent += metrics[i].Content + padding;
        }
        if (maxContent + padding <= equalWidth)
        {
            for (int j = 0; j < count; j++)
            {
                segmentWidths[j] = equalWidth;
            }
            return;
        }
        var shrink = ((totalContent > inner) ? (inner / totalContent) : 1f);
        var extra = MathF.Max(0f, inner - totalContent) / (float)count;
        for (int k = 0; k < count; k++)
        {
            segmentWidths[k] = (metrics[k].Content + padding) * shrink + extra;
        }
    }

    private static float SegmentOffset(int index)
    {
        var offset = 0f;
        for (int i = 0; i < index; i++)
        {
            offset += segmentWidths[i];
        }
        return offset;
    }

    private static Metrics Measure(Item item)
    {
        var iconSize = item.Icon is { } iconGlyph ? TextDraw.IconSize(iconGlyph) : Vector2.Zero;
        var label = TextDraw.Measure(item.Label);
        var content = label.X + (item.Icon.HasValue ? (iconSize.X + IconGap * ImGuiHelpers.GlobalScale) : 0f);
        return new Metrics(iconSize, label, content);
    }

    private static void DrawContent(Item item, Metrics metric, Vector2 segmentMin, float segmentWidth, float height, bool selected, float hover, bool enabled)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var iconSpace = (item.Icon.HasValue ? (metric.Icon.X + IconGap * globalScale) : 0f);
        var maxContent = segmentWidth - SegmentPadX * 2f * globalScale;
        var label = item.Label;
        var labelWidth = metric.Label.X;
        if (metric.Content > maxContent)
        {
            label = TextDraw.Truncate(label, MathF.Max(0f, maxContent - iconSpace));
            labelWidth = TextDraw.Measure(label).X;
        }
        var cursorX = segmentMin.X + (segmentWidth - iconSpace - labelWidth) * 0.5f;
        var midY = segmentMin.Y + height * 0.5f;
        var textColor = !enabled ? Styling.TextMuted : (selected ? Styling.TextStrong : Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover));
        if (item.Icon is { } iconGlyph)
        {
            var iconColor = !enabled ? Styling.TextMuted : (selected ? Styling.AccentGlowSoft : textColor);
            TextDraw.Icon(iconGlyph, new Vector2(cursorX, midY - metric.Icon.Y * 0.5f), iconColor);
            cursorX += iconSpace;
        }
        TextDraw.At(label, new Vector2(cursorX, midY - metric.Label.Y * 0.5f), textColor);
    }
}
