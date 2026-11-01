using System;
using System.Collections.Generic;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class FarmLibrary
{
    private const float RowHeight = 52f;

    private const float RowGap = 6f;

    private const string NpcMenuId = "##tta_farm_npc_menu";

    private const float SelectorRadius = 9f;

    private const float StopRowHeight = 40f;

    private const float SearchWidth = 240f;

    private const float CardSize = 32f;

    private const float CardGap = 4f;

    private const int ExpansionCount = 6;

    private static readonly FarmStopKind[] stops = new FarmStopKind[2]
    {
        FarmStopKind.Matches,
        FarmStopKind.Never
    };

    private static readonly Segmented.Item[] stopItems = new Segmented.Item[stops.Length];

    private static readonly List<ushort> visibleNpcs = new List<ushort>(160);

    private static string search = string.Empty;

    private static bool searchFocused;

    public static void Draw(Configuration configuration, AutoTriadController controller, bool scrollIntoView)
    {
        LibraryHeader.Draw(Loc.T(L.Farm.Title), scrollIntoView);
        Styling.VSpace(8f);
        if (!TriadData.Loaded)
        {
            TextDraw.Hint((TriadData.FailureReason.Length > 0) ? TriadData.FailureReason : Loc.T(L.Collection.Loading));
            return;
        }
        TriadOwnership.Refresh();
        var editable = !controller.Running;
        DrawStopRow(configuration, editable);
        Styling.VSpace(10f);
        var globalScale = ImGuiHelpers.GlobalScale;
        SearchField.Draw("##tta_farm_search", Loc.T(L.Collection.SearchHint), ref search, ref searchFocused, MathF.Min(SearchWidth * globalScale, ImGui.GetContentRegionAvail().X));
        Styling.VSpace(6f);
        var anyDrawn = false;
        for (var i = 0; i < ExpansionCount; i++)
        {
            Collect((byte)i);
            if (visibleNpcs.Count != 0)
            {
                GroupLabel.Draw(TriadLabels.Expansion((byte)i), anyDrawn ? 10f : 4f, 6f);
                anyDrawn = true;
                for (var j = 0; j < visibleNpcs.Count; j++)
                {
                    DrawRow(configuration, visibleNpcs[j], editable);
                    ImGui.Dummy(new Vector2(0f, RowGap * globalScale - ImGui.GetStyle().ItemSpacing.Y));
                }
            }
        }
        NpcSkipMenu.Draw(NpcMenuId, configuration);
        if (!anyDrawn)
        {
            TextDraw.Hint(Loc.T(L.Common.NoMatches, search));
        }
    }

    private static void DrawStopRow(Configuration configuration, bool editable)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        stopItems[0] = new Segmented.Item(null, Loc.T(L.Farm.StopAfterMatches));
        stopItems[1] = new Segmented.Item(null, Loc.T(L.Farm.StopNever));
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var rowHeight = StopRowHeight * globalScale;
        var label = Loc.T(L.Farm.StopLabel);
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(cursorScreenPos.X + 2f * globalScale, cursorScreenPos.Y + (rowHeight - labelSize.Y) * 0.5f), Styling.TextSecondary);
        var segmentedLeft = cursorScreenPos.X + labelSize.X + 14f * globalScale;
        var segmentedWidth = Segmented.PreferredWidth(stopItems);
        ImGui.SetCursorScreenPos(new Vector2(segmentedLeft, cursorScreenPos.Y));
        var selectedStop = Math.Max(0, Array.IndexOf(stops, configuration.FarmStop));
        if (Segmented.Draw("##tta_farm_stop", stopItems, ref selectedStop, editable, Layout.SegmentHeight, segmentedWidth))
        {
            configuration.FarmStop = stops[selectedStop];
            configuration.SaveDebounced();
        }
        if (configuration.FarmStop == FarmStopKind.Matches)
        {
            ImGui.SetCursorScreenPos(new Vector2(segmentedLeft + segmentedWidth + 12f * globalScale, cursorScreenPos.Y + (rowHeight - ImGui.GetFrameHeight()) * 0.5f));
            var matchLimit = configuration.FarmMatchLimit;
            if (Stepper.Draw("##tta_farm_limit", ref matchLimit, 5, 1, 9999, Loc.T(L.Farm.MatchesFormat)) & editable)
            {
                configuration.FarmMatchLimit = matchLimit;
                configuration.SaveDebounced();
            }
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, rowHeight));
    }

    private static void Collect(byte expansion)
    {
        visibleNpcs.Clear();
        var set = TriadData.Set;
        for (var i = 0; i < set.NpcCount; i++)
        {
            var npc = set.Npcs[i];
            if (Math.Min(npc.Expansion, (byte)(ExpansionCount - 1)) == expansion && (search.Length <= 0 || TriadLabels.NpcSearchKey(i).Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                visibleNpcs.Add((ushort)i);
            }
        }
    }

    private static void DrawRow(Configuration configuration, ushort npcIndex, bool editable)
    {
        var set = TriadData.Set;
        var npc = set.Npcs[npcIndex];
        var globalScale = ImGuiHelpers.GlobalScale;
        var rowSize = new Vector2(ImGui.GetContentRegionAvail().X, RowHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var rowMax = cursorScreenPos + rowSize;
        var skipReason = NpcEligibility.Check(npcIndex, configuration);
        var farmed = configuration.FarmNpcs.Contains(npc.TriadRowId);
        var selectable = editable && skipReason == SkipReason.None;
        ImGui.PushID((nint)npcIndex);
        var hit = Hit.Area("##farm", rowSize, selectable || (editable & farmed));
        var hover = Motion.Hover(Motion.Key("##farm"), hit.Hovered);
        var selection = Motion.Approach(Motion.Key("##farm", 1), farmed ? 1f : 0f);
        ImGui.PopID();
        if (hit.Clicked)
        {
            if (farmed)
            {
                configuration.FarmNpcs.Remove(npc.TriadRowId);
            }
            else
            {
                configuration.FarmNpcs.Add(npc.TriadRowId);
            }
            TriadLauncher.MarkSelectionChanged();
            configuration.SaveDebounced();
        }
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Glass(windowDrawList, cursorScreenPos, rowMax, Styling.CardRounding * globalScale, Styling.AccentGlow, 0.02f + 0.14f * selection, hover);
        var centerY = cursorScreenPos.Y + rowSize.Y * 0.5f;
        var center = new Vector2(cursorScreenPos.X + 14f * globalScale + SelectorRadius * globalScale, centerY);
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentGlowSoft, selection);
        windowDrawList.AddCircle(center, SelectorRadius * globalScale, Paint.Col(color), 0, 1.4f * globalScale);
        if (selection > 0.01f)
        {
            windowDrawList.AddCircleFilled(center, SelectorRadius * globalScale * selection, Paint.Col(Styling.AccentGlow));
        }
        var rewards = set.RewardsOf(npcIndex);
        var cardsWidth = (float)rewards.Length * (CardSize + CardGap) * globalScale;
        var cardsLeft = rowMax.X - 12f * globalScale - cardsWidth;
        for (var i = 0; i < rewards.Length; i++)
        {
            var cardId = rewards[i];
            CardIcon.Draw(windowDrawList, cardId, new Vector2(cardsLeft + (float)i * (CardSize + CardGap) * globalScale, centerY - CardSize * globalScale * 0.5f), CardSize * globalScale, TriadOwnership.IsOwned(cardId) ? CardIcon.State.Owned : CardIcon.State.Missing);
        }
        var textLeft = center.X + SelectorRadius * globalScale + 12f * globalScale;
        var textRight = cardsLeft - 10f * globalScale;
        if (skipReason != SkipReason.None)
        {
            textRight -= Badge.Draw(windowDrawList, TriadLabels.Skip(skipReason), Styling.AccentAmber, textRight, centerY) + 8f * globalScale;
        }
        var textLineHeight = ImGui.GetTextLineHeight();
        TextDraw.At(TextDraw.Truncate(set.NpcNames[npcIndex], textRight - textLeft), new Vector2(textLeft, centerY - textLineHeight + 1f * globalScale), Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selection, hover)));
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(TriadLabels.NpcLocation(npcIndex), textRight - textLeft), new Vector2(textLeft, centerY + 2f * globalScale), Styling.TextMuted);
        }
        NpcSkipMenu.OpenOnRightClick(NpcMenuId, npc.TriadRowId, cursorScreenPos, rowMax, editable);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(rowSize);
    }
}
