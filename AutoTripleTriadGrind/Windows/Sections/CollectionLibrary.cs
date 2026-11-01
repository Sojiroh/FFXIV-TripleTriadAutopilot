using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Planning;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections;

internal static class CollectionLibrary
{
    private const float ToolbarGap = 8f;

    private const float SearchWidth = 240f;

    private const float SummaryRowHeight = 32f;

    private const float NpcRowHeight = 62f;

    private const float NpcRowGap = 6f;

    private const string NpcMenuId = "##tta_collection_npc_menu";

    private const float CardSize = 40f;

    private const float CardGap = 5f;

    private const float CardTileWidth = 250f;

    private const float CardTileHeight = 56f;

    private const float SelectorRadius = 9f;

    private const float ListSlide = 8f;

    private const int ExpansionCount = 6;

    private static readonly LibraryView[] views = new LibraryView[2]
    {
        LibraryView.ByNpc,
        LibraryView.ByCard
    };

    private static readonly LibraryFilter[] filters = new LibraryFilter[3]
    {
        LibraryFilter.Missing,
        LibraryFilter.All,
        LibraryFilter.Owned
    };

    private static readonly Segmented.Item[] viewItems = new Segmented.Item[views.Length];

    private static readonly Segmented.Item[] filterItems = new Segmented.Item[filters.Length];

    private static readonly List<ushort> visibleNpcs = new List<ushort>(160);

    private static readonly List<ushort> visibleCards = new List<ushort>(480);

    private static readonly CachedText[] npcCounts = new CachedText[256];

    private static string search = string.Empty;

    private static bool searchFocused;

    private static CachedText summaryText;

    public static void Draw(Configuration configuration, AutoTriadController controller, bool scrollIntoView)
    {
        LibraryHeader.Draw(Loc.T(L.Collection.Title), scrollIntoView);
        Styling.VSpace(8f);
        if (!TriadData.Loaded)
        {
            TextDraw.Hint((TriadData.FailureReason.Length > 0) ? TriadData.FailureReason : Loc.T(L.Collection.Loading));
            return;
        }
        TriadOwnership.Refresh();
        DrawToolbar(configuration);
        Styling.VSpace(10f);
        var editable = !controller.Running;
        DrawSummaryRow(configuration, editable);
        Styling.VSpace(4f);
        using (Motion.PushSwitch("##tta_library_list", (int)((int)configuration.LibraryView * 8 + configuration.LibraryFilter), slide: ListSlide))
        {
            if (configuration.LibraryView == LibraryView.ByCard)
            {
                DrawCards(configuration, editable);
            }
            else
            {
                DrawNpcs(configuration, editable);
            }
        }
    }

    private static void DrawToolbar(Configuration configuration)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        viewItems[0] = new Segmented.Item(FontAwesomeIcon.User, Loc.T(L.Collection.ViewByNpc));
        viewItems[1] = new Segmented.Item(FontAwesomeIcon.ThLarge, Loc.T(L.Collection.ViewByCard));
        filterItems[0] = new Segmented.Item(null, Loc.T(L.Collection.FilterMissing));
        filterItems[1] = new Segmented.Item(null, Loc.T(L.Collection.FilterAll));
        filterItems[2] = new Segmented.Item(null, Loc.T(L.Collection.FilterOwned));
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var gap = ToolbarGap * globalScale;
        var viewWidth = Segmented.PreferredWidth(viewItems);
        var filterWidth = Segmented.PreferredWidth(filterItems);
        var searchWidth = MathF.Max(120f * globalScale, MathF.Min(SearchWidth * globalScale, availableWidth - viewWidth - filterWidth - gap * 2f));
        var barHeight = 36f;
        var selectedView = Array.IndexOf(views, configuration.LibraryView);
        if (Segmented.Draw("##tta_library_view", viewItems, ref selectedView, enabled: true, barHeight, viewWidth))
        {
            configuration.LibraryView = views[selectedView];
            configuration.SaveDebounced();
        }
        ImGui.SameLine(0f, gap);
        var selectedFilter = Array.IndexOf(filters, configuration.LibraryFilter);
        if (Segmented.Draw("##tta_library_filter", filterItems, ref selectedFilter, enabled: true, barHeight, filterWidth))
        {
            configuration.LibraryFilter = filters[selectedFilter];
            configuration.SaveDebounced();
        }
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + availableWidth - searchWidth, cursorScreenPos.Y + (barHeight * globalScale - SearchField.DefaultHeight * globalScale) * 0.5f));
        SearchField.Draw("##tta_library_search", Loc.T(L.Collection.SearchHint), ref search, ref searchFocused, searchWidth);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availableWidth, MathF.Max(barHeight * globalScale, SearchField.DefaultHeight * globalScale)));
    }

    private static void DrawSummaryRow(Configuration configuration, bool editable)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var set = TriadData.Set;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var rowHeight = SummaryRowHeight * globalScale;
        CountRewards(configuration, out var total, out var owned, out var selectedMissing);
        using (Fonts.PushCaption())
        {
            var key = ((long)selectedMissing << 40) | ((long)owned << 20) | (uint)total;
            var text = summaryText.Get(key, (long packed) => Loc.T(L.Collection.Summary, (int)(packed >> 40), (int)((packed >> 20) & 0xFFFFF), (int)(packed & 0xFFFFF)));
            var textSize = TextDraw.Measure(text);
            TextDraw.At(text, new Vector2(cursorScreenPos.X + 2f * globalScale, cursorScreenPos.Y + (rowHeight - textSize.Y) * 0.5f), Styling.TextDim);
        }
        var missingCount = total - owned;
        var allMissingSelected = missingCount > 0 && selectedMissing >= missingCount;
        var clearLabel = Loc.T(L.Common.Clear);
        var selectAllLabel = Loc.T(L.Collection.SelectAllMissing);
        var clearWidth = PillButton.Width(clearLabel);
        var selectAllWidth = PillButton.Width(selectAllLabel, FontAwesomeIcon.CheckDouble);
        var rightEdge = cursorScreenPos.X + availableWidth;
        ImGui.SetCursorScreenPos(new Vector2(rightEdge - clearWidth, cursorScreenPos.Y));
        if (PillButton.Draw("##tta_library_clear", clearLabel, Styling.AccentRose, PillButton.Emphasis.Ghost, enabled: editable && configuration.SelectedCards.Count > 0, height: SummaryRowHeight))
        {
            configuration.SelectedCards.Clear();
            OnSelectionChanged(configuration);
        }
        ImGui.SetCursorScreenPos(new Vector2(rightEdge - clearWidth - 6f * globalScale - selectAllWidth, cursorScreenPos.Y));
        if (PillButton.Draw("##tta_library_all", selectAllLabel, Styling.AccentGlow, PillButton.Emphasis.Tinted, FontAwesomeIcon.CheckDouble, editable && !allMissingSelected && missingCount > 0, SummaryRowHeight))
        {
            ushort[] cardIdsInOrder = set.CardIdsInOrder;
            foreach (var cardId in cardIdsInOrder)
            {
                if (set.IsNpcReward(cardId) && !TriadOwnership.IsOwned(cardId))
                {
                    configuration.SelectedCards.Add(cardId);
                }
            }
            OnSelectionChanged(configuration);
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(availableWidth, rowHeight));
    }

    private static void CountRewards(Configuration configuration, out int total, out int owned, out int selectedMissing)
    {
        var set = TriadData.Set;
        total = 0;
        owned = 0;
        selectedMissing = 0;
        ushort[] cardIdsInOrder = set.CardIdsInOrder;
        foreach (var cardId in cardIdsInOrder)
        {
            if (set.IsNpcReward(cardId))
            {
                total++;
                if (TriadOwnership.IsOwned(cardId))
                {
                    owned++;
                }
                else if (configuration.SelectedCards.Contains(cardId))
                {
                    selectedMissing++;
                }
            }
        }
    }

    private static void DrawNpcs(Configuration configuration, bool editable)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var anyDrawn = false;
        for (var i = 0; i < ExpansionCount; i++)
        {
            CollectNpcs(configuration, (byte)i);
            if (visibleNpcs.Count != 0)
            {
                GroupLabel.Draw(TriadLabels.Expansion((byte)i), anyDrawn ? 10f : 4f, 6f);
                anyDrawn = true;
                for (var j = 0; j < visibleNpcs.Count; j++)
                {
                    DrawNpcRow(configuration, visibleNpcs[j], editable);
                    ImGui.Dummy(new Vector2(0f, NpcRowGap * globalScale - ImGui.GetStyle().ItemSpacing.Y));
                }
            }
        }
        NpcSkipMenu.Draw(NpcMenuId, configuration);
        if (!anyDrawn)
        {
            TextDraw.Hint((search.Length > 0) ? Loc.T(L.Common.NoMatches, search) : Loc.T(L.Collection.NothingHere));
        }
    }

    private static void CollectNpcs(Configuration configuration, byte expansion)
    {
        visibleNpcs.Clear();
        var set = TriadData.Set;
        for (var i = 0; i < set.NpcCount; i++)
        {
            var npc = set.Npcs[i];
            if ((npc.Expansion == expansion || (expansion == ExpansionCount - 1 && npc.Expansion >= ExpansionCount)) && npc.RewardCount != 0)
            {
                var missingCount = TriadOwnership.MissingRewardCount(i);
                if (configuration.LibraryFilter switch
                {
                    LibraryFilter.Missing => missingCount > 0,
                    LibraryFilter.Owned => missingCount == 0,
                    _ => true,
                } && MatchesSearch(i))
                {
                    visibleNpcs.Add((ushort)i);
                }
            }
        }
    }

    private static bool MatchesSearch(int npcIndex)
    {
        if (search.Length == 0)
        {
            return true;
        }
        if (TriadLabels.NpcSearchKey(npcIndex).Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var rewards = TriadData.Set.RewardsOf(npcIndex);
        for (var i = 0; i < rewards.Length; i++)
        {
            if (TriadLabels.CardSearchKey(rewards[i]).Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static void DrawNpcRow(Configuration configuration, ushort npcIndex, bool editable)
    {
        var set = TriadData.Set;
        var globalScale = ImGuiHelpers.GlobalScale;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var rowSize = new Vector2(availableWidth, NpcRowHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var rowMax = cursorScreenPos + rowSize;
        var rewards = set.RewardsOf(npcIndex);
        var skipReason = NpcEligibility.Check(npcIndex, configuration);
        var eligible = skipReason == SkipReason.None;
        var missingCount = TriadOwnership.MissingRewardCount(npcIndex);
        var selectedMissing = CountSelectedMissing(configuration, rewards);
        var allSelected = missingCount > 0 && selectedMissing == missingCount;
        var windowDrawList = ImGui.GetWindowDrawList();
        ImGui.PushID((nint)npcIndex);
        var cardsWidth = (float)rewards.Length * (CardSize + CardGap) * globalScale - CardGap * globalScale;
        var cardsLeft = rowMax.X - 14f * globalScale - cardsWidth;
        var hit = Hit.Area("##npc", new Vector2(cardsLeft - cursorScreenPos.X - 8f * globalScale, rowSize.Y), editable && missingCount > 0);
        var hover = Motion.Hover(Motion.Key("##npc"), hit.Hovered);
        var selectionKey = Motion.Key("##npc", 1);
        var selection = Motion.Approach(selectionKey, allSelected ? 1f : ((selectedMissing > 0) ? 0.5f : 0f));
        if (hit.Clicked)
        {
            SetCards(configuration, rewards, !allSelected);
            OnSelectionChanged(configuration);
        }
        var accent = (eligible ? Styling.AccentGlow : Styling.AccentAmber);
        Paint.Glass(windowDrawList, cursorScreenPos, rowMax, Styling.CardRounding * globalScale, accent, 0.02f + 0.12f * selection, hover);
        var centerY = cursorScreenPos.Y + rowSize.Y * 0.5f;
        var center = new Vector2(cursorScreenPos.X + 14f * globalScale + SelectorRadius * globalScale, centerY);
        DrawSelector(windowDrawList, center, SelectorRadius * globalScale, missingCount == 0, selection);
        var textLeft = center.X + SelectorRadius * globalScale + 12f * globalScale;
        var textRight = cardsLeft - 12f * globalScale;
        var textLineHeight = ImGui.GetTextLineHeight();
        float captionLineHeight;
        using (Fonts.PushCaption())
        {
            captionLineHeight = ImGui.GetTextLineHeight();
        }
        var textTop = centerY - (textLineHeight + 3f * globalScale + captionLineHeight) * 0.5f;
        var counts = npcCounts[npcIndex % npcCounts.Length].Get((long)(((ulong)npcIndex << 32) | (ulong)((long)(rewards.Length - missingCount) << 16) | (uint)rewards.Length), (long key) => ((int)((key >> 16) & 0xFFFF)).ToString(Loc.Culture) + "/" + ((int)(key & 0xFFFF)).ToString(Loc.Culture));
        var cursorX = textRight;
        if (!eligible)
        {
            cursorX -= Badge.Draw(windowDrawList, TriadLabels.Skip(skipReason), Styling.AccentAmber, cursorX, textTop + textLineHeight * 0.5f) + 8f * globalScale;
        }
        using (Fonts.PushCaption())
        {
            var countsSize = TextDraw.Measure(counts);
            TextDraw.At(counts, new Vector2(cursorX - countsSize.X, textTop + (textLineHeight - countsSize.Y) * 0.5f), (missingCount == 0) ? Styling.AccentMint : Styling.TextDim);
            cursorX -= countsSize.X + 10f * globalScale;
        }
        var nameColor = Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selection, hover));
        TextDraw.At(TextDraw.Truncate(set.NpcNames[npcIndex], cursorX - textLeft), new Vector2(textLeft, textTop), (missingCount == 0) ? Styling.TextDim : nameColor);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(TriadLabels.NpcLocation(npcIndex), textRight - textLeft), new Vector2(textLeft, textTop + textLineHeight + 3f * globalScale), Styling.TextMuted);
        }
        if (hit.Hovered)
        {
            DrawNpcTooltip(configuration, npcIndex, skipReason);
        }
        var y = centerY - CardSize * globalScale * 0.5f;
        for (var i = 0; i < rewards.Length; i++)
        {
            ImGui.SetCursorScreenPos(new Vector2(cardsLeft + (float)i * (CardSize + CardGap) * globalScale, y));
            DrawCard(configuration, rewards[i], editable);
        }
        ImGui.PopID();
        NpcSkipMenu.OpenOnRightClick(NpcMenuId, set.Npcs[npcIndex].TriadRowId, cursorScreenPos, rowMax, editable);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(rowSize);
    }

    private static void DrawCard(Configuration configuration, ushort cardId, bool editable)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cardSize = CardSize * globalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var owned = TriadOwnership.IsOwned(cardId);
        var selected = configuration.SelectedCards.Contains(cardId);
        ImGui.PushID((nint)cardId);
        var hit = Hit.Area("##card", new Vector2(cardSize, cardSize), editable && !owned);
        var hover = Motion.Hover(Motion.Key("##card"), Hit.HoveringRect(cursorScreenPos, cursorScreenPos + new Vector2(cardSize, cardSize)));
        ImGui.PopID();
        if (hit.Clicked)
        {
            ToggleCard(configuration, cardId, !selected);
        }
        var state = (owned ? CardIcon.State.Owned : (selected ? CardIcon.State.Selected : CardIcon.State.Missing));
        CardIcon.Draw(ImGui.GetWindowDrawList(), cardId, cursorScreenPos, cardSize, state, hover);
        if (Hit.HoveringRect(cursorScreenPos, cursorScreenPos + new Vector2(cardSize, cardSize)))
        {
            DrawCardTooltip(cardId, owned, selected);
        }
    }

    private static void DrawCards(Configuration configuration, bool editable)
    {
        CollectCards(configuration);
        if (visibleCards.Count == 0)
        {
            TextDraw.Hint((search.Length > 0) ? Loc.T(L.Common.NoMatches, search) : Loc.T(L.Collection.NothingHere));
            return;
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var gap = 6f * globalScale;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((availableWidth + gap) / (CardTileWidth * globalScale + gap)));
        var width = (availableWidth - gap * (float)(columns - 1)) / (float)columns;
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap)))
        {
            for (var i = 0; i < visibleCards.Count; i++)
            {
                if (i % columns != 0)
                {
                    ImGui.SameLine(0f, gap);
                }
                DrawCardTile(configuration, visibleCards[i], width, editable);
            }
        }
    }

    private static void CollectCards(Configuration configuration)
    {
        visibleCards.Clear();
        var set = TriadData.Set;
        ushort[] cardIdsInOrder = set.CardIdsInOrder;
        foreach (var cardId in cardIdsInOrder)
        {
            if (set.IsNpcReward(cardId))
            {
                var owned = TriadOwnership.IsOwned(cardId);
                if (configuration.LibraryFilter switch
                {
                    LibraryFilter.Missing => !owned,
                    LibraryFilter.Owned => owned,
                    _ => true,
                } && (search.Length <= 0 || CardMatchesSearch(cardId)))
                {
                    visibleCards.Add(cardId);
                }
            }
        }
    }

    private static bool CardMatchesSearch(ushort cardId)
    {
        if (TriadLabels.CardSearchKey(cardId).Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var npcs = TriadData.Set.NpcsDropping(cardId);
        for (var i = 0; i < npcs.Length; i++)
        {
            if (TriadLabels.NpcSearchKey(npcs[i]).Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static void DrawCardTile(Configuration configuration, ushort cardId, float width, bool editable)
    {
        var set = TriadData.Set;
        var globalScale = ImGuiHelpers.GlobalScale;
        var tileSize = new Vector2(width, CardTileHeight * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + tileSize;
        var owned = TriadOwnership.IsOwned(cardId);
        var selected = configuration.SelectedCards.Contains(cardId);
        ImGui.PushID((nint)cardId);
        var hit = Hit.Area("##tile", tileSize, editable && !owned);
        var hover = Motion.Hover(Motion.Key("##tile"), hit.Hovered);
        var selection = Motion.Approach(Motion.Key("##tile", 1), (selected && !owned) ? 1f : 0f);
        ImGui.PopID();
        if (hit.Clicked)
        {
            ToggleCard(configuration, cardId, !selected);
        }
        var windowDrawList = ImGui.GetWindowDrawList();
        Paint.Glass(windowDrawList, cursorScreenPos, max, Styling.CardRounding * globalScale, owned ? Styling.AccentMint : Styling.AccentGlow, 0.02f + 0.14f * selection, hover);
        var iconSize = CardTileHeight * globalScale - 12f * globalScale;
        var min = new Vector2(cursorScreenPos.X + 6f * globalScale, cursorScreenPos.Y + 6f * globalScale);
        CardIcon.Draw(windowDrawList, cardId, min, iconSize, owned ? CardIcon.State.Owned : (selected ? CardIcon.State.Selected : CardIcon.State.Missing), hover);
        var textLeft = min.X + iconSize + 10f * globalScale;
        var maxWidth = max.X - 10f * globalScale - textLeft;
        var textLineHeight = ImGui.GetTextLineHeight();
        var centerY = cursorScreenPos.Y + tileSize.Y * 0.5f;
        float captionLineHeight;
        using (Fonts.PushCaption())
        {
            captionLineHeight = ImGui.GetTextLineHeight();
        }
        var textTop = centerY - (textLineHeight + 2f * globalScale + captionLineHeight) * 0.5f;
        TextDraw.At(TextDraw.Truncate(set.CardNames[cardId], maxWidth), new Vector2(textLeft, textTop), owned ? Styling.TextDim : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selection, hover)));
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(TriadLabels.CardStats(cardId), maxWidth), new Vector2(textLeft, textTop + textLineHeight + 2f * globalScale), Styling.TextMuted);
        }
        if (hit.Hovered || Hit.HoveringRect(cursorScreenPos, max))
        {
            DrawCardTooltip(cardId, owned, selected);
        }
    }

    private static void DrawSelector(ImDrawListPtr drawList, Vector2 center, float radius, bool complete, float active)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        if (complete)
        {
            drawList.AddCircleFilled(center, radius, Paint.Col(Styling.WithAlpha(Styling.AccentMint, 0.85f)));
            Paint.Check(drawList, center, radius * 1.1f, Styling.InkOnGlow, 1.8f * globalScale);
            return;
        }
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentGlowSoft, active);
        drawList.AddCircle(center, radius, Paint.Col(color), 0, 1.4f * globalScale);
        if (active <= 0.01f)
        {
            return;
        }
        drawList.AddCircleFilled(center, radius * active, Paint.Col(Styling.AccentGlow));
        if (active > 0.75f)
        {
            Paint.Check(drawList, center, radius * 1.1f, Styling.WithAlpha(Styling.InkOnGlow, (active - 0.75f) * 4f), 1.8f * globalScale);
        }
    }

    private static void DrawNpcTooltip(Configuration configuration, int npcIndex, SkipReason eligibility)
    {
        var set = TriadData.Set;
        var npc = set.Npcs[npcIndex];
        using (Tooltip.Begin())
        {
            Tooltip.Text(set.NpcNames[npcIndex], Styling.TextStrong);
            Tooltip.Text(TriadLabels.NpcLocation(npcIndex), Styling.TextDim);
            Tooltip.Text(Loc.T(L.Collection.TooltipFee, npc.Fee), Styling.TextSecondary);
            if (npc.RuleMask != 0)
            {
                Tooltip.Text(Loc.T(L.Collection.TooltipRules, RuleList(npc.RuleMask)), Styling.TextSecondary);
            }
            if (npc.UsesRegionalRules)
            {
                Tooltip.Text(Loc.T(L.Collection.TooltipRegional), Styling.TextDim);
            }
            if (eligibility == SkipReason.Locked && LockedQuestText(npcIndex) is { Length: > 0 } text)
            {
                Tooltip.Text(text, Styling.AccentAmber);
            }
            else if (eligibility != SkipReason.None)
            {
                Tooltip.Text(TriadLabels.Skip(eligibility), Styling.AccentAmber);
            }
            Tooltip.Text(Loc.T(L.Collection.TooltipSelectNpc), Styling.TextMuted);
            Tooltip.Text(Loc.T(configuration.SkippedNpcs.Contains(TriadData.Set.Npcs[npcIndex].TriadRowId) ? L.Collection.TooltipUnskipNpc : L.Collection.TooltipSkipNpc), Styling.TextMuted);
        }
    }

    private static string LockedQuestText(int npcIndex)
    {
        var slot = TriadUnlock.MissingQuestSlot(npcIndex);
        if (slot == -1)
        {
            return string.Empty;
        }
        var set = TriadData.Set;
        var questNames = set.UnlockQuestNamesOf(npcIndex);
        if (!set.Npcs[npcIndex].UnlockNeedsAnyQuest)
        {
            if (questNames[slot].Length != 0)
            {
                return Loc.T(L.Collection.TooltipLockedQuest, questNames[slot]);
            }
            return string.Empty;
        }
        var names = new StringBuilder();
        for (var i = 0; i < questNames.Length; i++)
        {
            if (questNames[i].Length != 0)
            {
                if (names.Length > 0)
                {
                    names.Append(" / ");
                }
                names.Append(questNames[i]);
            }
        }
        if (names.Length != 0)
        {
            return Loc.T(L.Collection.TooltipLockedAnyQuest, names.ToString());
        }
        return string.Empty;
    }

    private static void DrawCardTooltip(ushort cardId, bool owned, bool selected)
    {
        var set = TriadData.Set;
        using (Tooltip.Begin())
        {
            Tooltip.Text(set.CardNames[cardId], Styling.TextStrong);
            Tooltip.Text(TriadLabels.CardStats(cardId), Styling.TextDim);
            Tooltip.Text(owned ? Loc.T(L.Collection.TooltipOwned) : (selected ? Loc.T(L.Collection.TooltipSelected) : Loc.T(L.Collection.TooltipMissing)), owned ? Styling.AccentMint : (selected ? Styling.AccentGlowSoft : Styling.TextSecondary));
            var npcs = set.NpcsDropping(cardId);
            for (var i = 0; i < npcs.Length; i++)
            {
                Tooltip.Text(Loc.T(L.Collection.TooltipDroppedBy, set.NpcNames[npcs[i]], TriadLabels.NpcLocation(npcs[i])), Styling.TextSecondary);
            }
        }
    }

    private static string RuleList(ushort ruleMask)
    {
        string[] ruleNames = TriadData.Set.RuleNames;
        var rules = string.Empty;
        for (var i = 1; i < 16; i++)
        {
            if (TriadRuleIds.Has(ruleMask, (TriadRuleId)i))
            {
                rules = ((rules.Length == 0) ? ruleNames[i] : (rules + ", " + ruleNames[i]));
            }
        }
        return rules;
    }

    private static int CountSelectedMissing(Configuration configuration, ReadOnlySpan<ushort> rewards)
    {
        var count = 0;
        for (var i = 0; i < rewards.Length; i++)
        {
            if (!TriadOwnership.IsOwned(rewards[i]) && configuration.SelectedCards.Contains(rewards[i]))
            {
                count++;
            }
        }
        return count;
    }

    private static void SetCards(Configuration configuration, ReadOnlySpan<ushort> rewards, bool selected)
    {
        for (var i = 0; i < rewards.Length; i++)
        {
            var cardId = rewards[i];
            if (!TriadOwnership.IsOwned(cardId))
            {
                if (selected)
                {
                    configuration.SelectedCards.Add(cardId);
                }
                else
                {
                    configuration.SelectedCards.Remove(cardId);
                }
            }
        }
    }

    private static void ToggleCard(Configuration configuration, ushort cardId, bool selected)
    {
        if (selected)
        {
            configuration.SelectedCards.Add(cardId);
        }
        else
        {
            configuration.SelectedCards.Remove(cardId);
        }
        OnSelectionChanged(configuration);
    }

    private static void OnSelectionChanged(Configuration configuration)
    {
        TriadLauncher.MarkSelectionChanged();
        configuration.SaveDebounced();
    }
}
