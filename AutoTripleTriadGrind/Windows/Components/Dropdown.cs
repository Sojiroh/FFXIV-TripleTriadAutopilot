using System;
using System.Collections.Generic;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Components;

internal static class Dropdown
{
    private sealed class State
    {
        public readonly string PopupId;

        public long OpenedTick;

        public string Filter = string.Empty;

        public int Highlight;

        public bool ScrollToHighlight;

        public State(string popupId)
        {
            PopupId = popupId;
        }
    }

    private const float TriggerPadX = 11f;

    private const float CaretHalf = 4.5f;

    private const float CaretThickness = 1.7f;

    private const float CaretGap = 9f;

    private const float PanelGap = 6f;

    private const float PanelPad = 6f;

    private const float PanelMaxList = 300f;

    private const float PanelCollapsed = 0.42f;

    private const float PanelShadow = 14f;

    private const float ViewportMargin = 6f;

    private const float RowPadX = 11f;

    private const float RowPadY = 8f;

    private const float RowGap = 2f;

    private const float RowRounding = 8f;

    private const float RowSlide = 9f;

    private const float RailWidth = 3f;

    private const float RailInset = 4f;

    private const float DetailGap = 3f;

    private const float CheckSize = 9f;

    private const float SearchHeight = 30f;

    private const float SearchPadX = 9f;

    private const float SearchIconGap = 7f;

    private const int FilterMaxLength = 64;

    private const float RevealMs = 200f;

    private const float StaggerMs = 14f;

    private const int StaggerRows = 10;

    private const string ListId = "##tta_dropdown_list";

    private const string RowId = "##tta_dropdown_row";

    private const string SearchId = "##tta_dropdown_search";

    private const ImGuiWindowFlags PanelFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoSavedSettings;

    private static readonly Dictionary<string, State> states = new Dictionary<string, State>(StringComparer.Ordinal);

    private static int[] rowIndices = new int[64];

    private static float[] rowHeights = new float[64];

    public static bool Draw(string id, ReadOnlySpan<string> labels, ref int selected, float width, float panelWidth = 0f, bool searchable = false, string? searchHint = null) => DrawCore(id, labels, default, ref selected, width, panelWidth, searchable, searchHint);

    public static bool DrawDetailed(string id, ReadOnlySpan<string> labels, ReadOnlySpan<string> details, ref int selected, float width, float panelWidth = 0f) => DrawCore(id, labels, details, ref selected, width, panelWidth, searchable: false, null);

    private static bool DrawCore(string id, ReadOnlySpan<string> labels, ReadOnlySpan<string> details, ref int selected, float width, float panelWidth, bool searchable, string? searchHint)
    {
        if (labels.Length == 0)
        {
            return false;
        }
        selected = Math.Clamp(selected, 0, labels.Length - 1);
        var state = StateFor(id);
        var isOpen = ImGui.IsPopupOpen(state.PopupId);
        var triggerSize = new Vector2(width * ImGuiHelpers.GlobalScale, ImGui.GetFrameHeight());
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        if (DrawTrigger(id, labels[selected], cursorScreenPos, triggerSize, isOpen) && !isOpen)
        {
            ImGui.OpenPopup(state.PopupId);
            state.OpenedTick = Environment.TickCount64;
            state.Filter = string.Empty;
            state.Highlight = selected;
            state.ScrollToHighlight = true;
            isOpen = true;
        }
        return isOpen && DrawPanel(state, labels, details, ref selected, cursorScreenPos, triggerSize, panelWidth, searchable, searchHint);
    }

    private static State StateFor(string id)
    {
        if (states.TryGetValue(id, out var state))
        {
            return state;
        }
        state = new State(id + "_panel");
        states[id] = state;
        return state;
    }

    private static bool DrawTrigger(string id, string label, Vector2 origin, Vector2 size, bool open)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var hit = Hit.Area(id, size);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var openAmount = Motion.Approach(Motion.Key(id, 1), open ? 1f : 0f, 16f);
        var amount = MathF.Max(hover * 0.55f, openAmount);
        var windowDrawList = ImGui.GetWindowDrawList();
        var max = origin + size;
        var rounding = Styling.FrameRounding * globalScale;
        if (openAmount > 0.01f)
        {
            Paint.Glow(windowDrawList, origin, max, rounding, Styling.AccentGlow, openAmount);
        }
        var topColor = Styling.Tint(Vector4.Lerp(Styling.Surface2, Styling.Surface3, hover), Styling.AccentGlow, openAmount * 0.22f);
        var bottomColor = Styling.Tint(Vector4.Lerp(Styling.Surface1, Styling.Surface2, hover), Styling.AccentGlow, openAmount * 0.12f);
        if (hit.Held)
        {
            topColor = Styling.Darken(topColor, 0.1f);
            bottomColor = Styling.Darken(bottomColor, 0.1f);
        }
        Paint.Gradient(windowDrawList, origin, max, topColor, bottomColor, rounding);
        Paint.TopLight(windowDrawList, origin, max, rounding);
        Paint.Stroke(windowDrawList, origin, max, Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.85f), Styling.WithAlpha(Styling.AccentGlowSoft, 0.9f), amount), rounding);
        var padX = TriggerPadX * globalScale;
        var caretHalf = CaretHalf * globalScale;
        var center = new Vector2(max.X - padX - caretHalf, origin.Y + size.Y * 0.5f);
        var maxWidth = center.X - caretHalf - CaretGap * globalScale - origin.X - padX;
        var truncated = TextDraw.Truncate(label, maxWidth);
        var textSize = TextDraw.Measure(truncated);
        TextDraw.At(truncated, new Vector2(origin.X + padX, origin.Y + (size.Y - textSize.Y) * 0.5f), Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, amount));
        DrawCaret(windowDrawList, center, caretHalf, openAmount, Vector4.Lerp(Styling.TextDim, Styling.AccentGlowSoft, amount), CaretThickness * globalScale);
        return hit.Hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }

    private static void DrawCaret(ImDrawListPtr dl, Vector2 center, float half, float flip, Vector4 color, float thickness)
    {
        var dip = half * 0.55f * (1f - 2f * flip);
        var left = center + new Vector2(-half, -dip);
        var bottom = center + new Vector2(0f, dip);
        var right = center + new Vector2(half, -dip);
        var packed = Paint.Col(color);
        dl.AddLine(left, bottom, packed, thickness);
        dl.AddLine(bottom, right, packed, thickness);
    }

    private static bool DrawPanel(State state, ReadOnlySpan<string> labels, ReadOnlySpan<string> details, ref int selected, Vector2 triggerOrigin, Vector2 triggerSize, float panelWidth, bool searchable, string? searchHint)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var pad = PanelPad * globalScale;
        var panelWidthPx = MathF.Max(triggerSize.X, panelWidth * globalScale);
        var innerWidth = panelWidthPx - pad * 2f;
        var rows = BuildRows(labels, details, state.Filter, innerWidth - RowPadX * 2f * globalScale, globalScale, out var total);
        var searchBlock = (searchable ? (SearchHeight * globalScale + pad) : 0f);
        var listHeight = MathF.Min(total, PanelMaxList * globalScale);
        var contentHeight = pad * 2f + searchBlock + listHeight;
        var reveal = Motion.Reveal(state.OpenedTick, RevealMs);
        var height = MathF.Max(1f, contentHeight * (PanelCollapsed + (1f - PanelCollapsed) * reveal));
        ImGui.SetNextWindowPos(PanelPosition(triggerOrigin, triggerSize, panelWidthPx, contentHeight, height, globalScale));
        ImGui.SetNextWindowSize(new Vector2(panelWidthPx, height));
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero).Push(ImGuiStyleVar.PopupRounding, 10f * globalScale).Push(ImGuiStyleVar.PopupBorderSize, 0f)
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, RowGap * globalScale))
            .Push(ImGuiStyleVar.Alpha, MathF.Max(0.05f, reveal)))
        {
            using (ImRaii.PushColor(ImGuiCol.PopupBg, Vector4.Zero))
            {
                using var popup = ImRaii.Popup(state.PopupId, PanelFlags);
                if (!popup)
                {
                    return false;
                }
                PaintPanel(globalScale);
                var innerOrigin = ImGui.GetWindowPos() + new Vector2(pad, pad);
                if (searchable)
                {
                    DrawSearch(state, innerOrigin, innerWidth, searchHint, globalScale);
                }
                var origin = innerOrigin + new Vector2(0f, searchBlock);
                if (rows == 0)
                {
                    DrawNoMatches(state, origin, innerWidth, globalScale);
                    return false;
                }
                return DrawRows(state, labels, details, ref selected, rows, origin, innerWidth, listHeight, globalScale);
            }
        }
    }

    private static Vector2 PanelPosition(Vector2 triggerOrigin, Vector2 triggerSize, float width, float contentHeight, float height, float scale)
    {
        var mainViewport = ImGui.GetMainViewport();
        var margin = ViewportMargin * scale;
        var gap = PanelGap * scale;
        var x = ((width > triggerSize.X) ? (triggerOrigin.X + triggerSize.X - width) : triggerOrigin.X);
        x = MathF.Max(mainViewport.WorkPos.X + margin, MathF.Min(x, mainViewport.WorkPos.X + mainViewport.WorkSize.X - width - margin));
        var belowY = triggerOrigin.Y + triggerSize.Y + gap;
        var overflowsBelow = belowY + contentHeight > mainViewport.WorkPos.Y + mainViewport.WorkSize.Y - margin;
        var fitsAbove = triggerOrigin.Y - gap - contentHeight > mainViewport.WorkPos.Y + margin;
        return new Vector2(x, (overflowsBelow & fitsAbove) ? (triggerOrigin.Y - gap - height) : belowY);
    }

    private static void PaintPanel(float scale)
    {
        var windowDrawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var max = windowPos + ImGui.GetWindowSize();
        var rounding = 10f * scale;
        var shadowSize = PanelShadow * scale;
        var shadowExtent = new Vector2(shadowSize * 3f, shadowSize * 3f);
        windowDrawList.PushClipRect(windowPos - shadowExtent, max + shadowExtent, intersectWithCurrentClipRect: false);
        Paint.Shadow(windowDrawList, windowPos, max, rounding, shadowSize, 0.55f);
        windowDrawList.PopClipRect();
        Paint.Gradient(windowDrawList, windowPos, max, Styling.Surface2 with { W = 0.99f }, Styling.Surface0 with { W = 0.99f }, rounding);
        Paint.TopLight(windowDrawList, windowPos, max, rounding, 0.09f);
        Paint.Stroke(windowDrawList, windowPos, max, Styling.WithAlpha(Styling.AccentGlow, 0.42f), rounding);
    }

    private static int BuildRows(ReadOnlySpan<string> labels, ReadOnlySpan<string> details, string filter, float wrapWidth, float scale, out float total)
    {
        EnsureCapacity(labels.Length);
        var baseHeight = RowPadY * 2f * scale + ImGui.GetTextLineHeight();
        var gap = RowGap * scale;
        var rowCount = 0;
        total = 0f;
        using (details.IsEmpty ? null : Fonts.PushCaption())
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (filter.Length <= 0 || labels[i].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var rowHeight = (details.IsEmpty ? baseHeight : (baseHeight + DetailGap * scale + TextDraw.MeasureWrapped(details[i], wrapWidth).Y));
                    rowIndices[rowCount] = i;
                    rowHeights[rowCount] = rowHeight;
                    total += ((rowCount > 0) ? (rowHeight + gap) : rowHeight);
                    rowCount++;
                }
            }
            if (rowCount == 0)
            {
                total = baseHeight;
            }
            return rowCount;
        }
    }

    private static void EnsureCapacity(int count)
    {
        if (rowIndices.Length >= count)
        {
            return;
        }
        var capacity = Math.Max(count, rowIndices.Length * 2);
        rowIndices = new int[capacity];
        rowHeights = new float[capacity];
    }

    private static bool DrawRows(State state, ReadOnlySpan<string> labels, ReadOnlySpan<string> details, ref int selected, int rows, Vector2 origin, float innerWidth, float listHeight, float scale)
    {
        state.Highlight = Math.Clamp(state.Highlight, 0, rows - 1);
        var enterPressed = HandleKeys(state, rows);
        ImGui.SetCursorScreenPos(origin);
        using var list = ImRaii.Child(ListId, new Vector2(innerWidth, listHeight), border: false, ImGuiWindowFlags.NoBackground);
        if (!list)
        {
            return false;
        }
        if (state.ScrollToHighlight)
        {
            ImGui.SetScrollY(HighlightScroll(state.Highlight, listHeight, RowGap * scale));
            state.ScrollToHighlight = false;
        }
        var rowWidth = ImGui.GetContentRegionAvail().X;
        var scrollY = ImGui.GetScrollY();
        var chosen = -1;
        for (int i = 0; i < rows; i++)
        {
            var rowHeight = rowHeights[i];
            var cursorPosY = ImGui.GetCursorPosY();
            if (cursorPosY + rowHeight < scrollY || cursorPosY > scrollY + listHeight)
            {
                ImGui.Dummy(new Vector2(rowWidth, rowHeight));
                continue;
            }
            var labelIndex = rowIndices[i];
            var detail = (details.IsEmpty ? null : details[labelIndex]);
            if (DrawRow(state, i, labels[labelIndex], detail, labelIndex == selected, rowWidth, rowHeight, scale))
            {
                chosen = labelIndex;
            }
        }
        if (enterPressed)
        {
            chosen = rowIndices[state.Highlight];
        }
        if (chosen < 0)
        {
            return false;
        }
        ImGui.CloseCurrentPopup();
        if (chosen == selected)
        {
            return false;
        }
        selected = chosen;
        return true;
    }

    private static bool HandleKeys(State state, int rows)
    {
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, repeat: true))
        {
            state.Highlight = (state.Highlight + 1) % rows;
            state.ScrollToHighlight = true;
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, repeat: true))
        {
            state.Highlight = (state.Highlight + rows - 1) % rows;
            state.ScrollToHighlight = true;
        }
        return ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);
    }

    private static float HighlightScroll(int highlight, float listHeight, float gap)
    {
        var num = 0f;
        for (int i = 0; i < highlight; i++)
        {
            num += rowHeights[i] + gap;
        }
        return MathF.Max(0f, num - (listHeight - rowHeights[highlight]) * 0.5f);
    }

    private static bool DrawRow(State state, int row, string name, string? detail, bool selected, float width, float height, float scale)
    {
        var size = new Vector2(width, height);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        ImGui.PushID(row);
        var hit = Hit.Area(RowId, size);
        ImGui.PopID();
        if (hit.Hovered)
        {
            state.Highlight = row;
        }
        var hover = Motion.Hover(Motion.Key(RowId, row), hit.Hovered || state.Highlight == row);
        var reveal = Motion.Reveal(state.OpenedTick, RevealMs, MathF.Min(row, StaggerRows) * StaggerMs);
        var windowDrawList = ImGui.GetWindowDrawList();
        var rowMin = cursorScreenPos + new Vector2((1f - reveal) * RowSlide * scale, 0f);
        var max = rowMin + size;
        var color = (selected ? Styling.WithAlpha(Styling.AccentGlow, (0.17f + 0.1f * hover) * reveal) : Styling.WithAlpha(Styling.Surface3, 0.75f * hover * reveal));
        if (color.W > 0.004f)
        {
            Paint.Fill(windowDrawList, rowMin, max, color, RowRounding * scale);
        }
        var padX = RowPadX * scale;
        var padY = RowPadY * scale;
        var textLineHeight = ImGui.GetTextLineHeight();
        var checkSize = CheckSize * scale;
        if (selected)
        {
            var railInset = RailInset * scale;
            var min = new Vector2(rowMin.X + railInset, rowMin.Y + railInset);
            Paint.Fill(max: new Vector2(min.X + RailWidth * scale, max.Y - railInset), dl: windowDrawList, min: min, color: Styling.WithAlpha(Styling.AccentGlowSoft, reveal), rounding: RailWidth * scale * 0.5f);
            Paint.Check(windowDrawList, new Vector2(max.X - padX - checkSize * 0.5f, rowMin.Y + padY + textLineHeight * 0.5f), checkSize, Styling.WithAlpha(Styling.AccentGlowSoft, reveal), 2f * scale);
        }
        var textLeft = rowMin.X + padX;
        var textRight = max.X - padX - (selected ? (checkSize * 2f) : 0f);
        var textColor = (selected ? Styling.AccentGlowSoft : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover));
        TextDraw.At(TextDraw.Truncate(name, textRight - textLeft), new Vector2(textLeft, rowMin.Y + padY), Styling.WithAlpha(textColor, reveal));
        if (detail != null)
        {
            using (Fonts.PushCaption())
            {
                TextDraw.Wrapped(detail, new Vector2(textLeft, rowMin.Y + padY + textLineHeight + DetailGap * scale), max.X - padX - textLeft, Styling.WithAlpha(Styling.TextMuted, reveal));
            }
        }
        return hit.Clicked;
    }

    private static void DrawSearch(State state, Vector2 origin, float width, string? hint, float scale)
    {
        var height = SearchHeight * scale;
        var padX = SearchPadX * scale;
        var max = origin + new Vector2(width, height);
        var windowDrawList = ImGui.GetWindowDrawList();
        var rounding = Styling.FrameRounding * scale;
        Paint.Fill(windowDrawList, origin, max, Styling.WithAlpha(Styling.Surface0, 0.85f), rounding);
        Paint.Stroke(windowDrawList, origin, max, Styling.WithAlpha(Styling.BorderDim, 0.7f), rounding);
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Search);
        TextDraw.Icon(FontAwesomeIcon.Search, new Vector2(origin.X + padX, origin.Y + (height - iconSize.Y) * 0.5f), Styling.TextMuted);
        var inputX = origin.X + padX + iconSize.X + SearchIconGap * scale;
        ImGui.SetCursorScreenPos(new Vector2(inputX, origin.Y + (height - ImGui.GetFrameHeight()) * 0.5f));
        ImGui.SetNextItemWidth(max.X - inputX - padX);
        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere(0);
        }
        var buf = state.Filter;
        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero).Push(ImGuiCol.FrameBgHovered, Vector4.Zero).Push(ImGuiCol.FrameBgActive, Vector4.Zero))
        {
            ImGui.InputTextWithHint(SearchId, hint ?? Loc.T(L.Settings.SearchHint), ref buf, FilterMaxLength);
        }
        if (string.Equals(buf, state.Filter, StringComparison.Ordinal))
        {
            return;
        }
        state.Filter = buf;
        state.Highlight = 0;
        state.ScrollToHighlight = true;
    }

    private static void DrawNoMatches(State state, Vector2 origin, float width, float scale)
    {
        using (Fonts.PushCaption())
        {
            var text = Loc.T(L.Common.NoMatches, state.Filter);
            var padX = RowPadX * scale;
            TextDraw.At(TextDraw.Truncate(text, width - padX * 2f), origin + new Vector2(padX, RowPadY * scale), Styling.TextMuted);
        }
    }
}
