using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Triad.Data;

internal static class TriadDataLoader
{
    private readonly record struct TriadENpc(uint ENpcId, uint TriadRowId);

    private readonly record struct NpcLevel(Vector3 Position, uint MapId, uint TerritoryId, bool OnEventMap);

    private const byte LevelTypeNpc = 8;

    private const byte PreviousQuestJoinAny = 2;

    private const int CardTypeRows = 5;

    private const int CardRarityRows = 6;

    private const uint TriadCardItemCategory = 86u;

    public const uint BattleHallTerritoryId = 579u;

    private static readonly string[] casingExclusions = new string[9] { "the", "goe", "van", "des", "sas", "yae", "tol", "der", "rem" };

    public static bool TryLoad(out TriadDataSet loaded, out string failure)
    {
        loaded = TriadDataSet.Empty;
        try
        {
            return TryBuild(out loaded, out failure);
        }
        catch (Exception ex)
        {
            failure = "Reading the Triple Triad game data failed: " + ex.Message;
            RunLog.Error(ex, failure);
            return false;
        }
    }

    private static bool TryBuild(out TriadDataSet loaded, out string failure)
    {
        loaded = TriadDataSet.Empty;
        var ruleSheet = Svc.Data.GetExcelSheet<TripleTriadRule>();
        if (ruleSheet.Count != TriadRuleIds.Count)
        {
            failure = $"The game has {ruleSheet.Count} Triple Triad rules where {TriadRuleIds.Count} are known; the plugin needs an update.";
            return false;
        }
        if (Svc.Data.GetExcelSheet<TripleTriadCardType>().Count != CardTypeRows || Svc.Data.GetExcelSheet<TripleTriadCardRarity>().Count != CardRarityRows)
        {
            failure = "The game's card type or rarity tables changed shape; the plugin needs an update.";
            return false;
        }
        var ruleNames = new string[TriadRuleIds.Count];
        for (var i = 0; i < TriadRuleIds.Count; i++)
        {
            ruleNames[i] = ruleSheet.GetRowOrDefault((uint)i)?.Name.ExtractText() ?? ((TriadRuleId)i).ToString();
        }
        if (!TryReadCards(out var cards, out var names, out failure))
        {
            return false;
        }
        var cardItemIds = new uint[cards.Length];
        var cardIdByItemId = ReadCardItems(cards, cardItemIds);
        List<TriadNpc> npcList = new();
        List<string> npcNames = new();
        List<uint> unlockQuests = new();
        List<string> unlockQuestNames = new();
        List<ushort> deckCards = new();
        List<ushort> rewardCardList = new();
        ReadNpcs(cards, cardIdByItemId, npcList, npcNames, unlockQuests, unlockQuestNames, deckCards, rewardCardList);
        var npcs = npcList.ToArray();
        Dictionary<uint, ushort> npcIndexByTriadRowId = new(npcs.Length);
        for (var j = 0; j < npcs.Length; j++)
        {
            npcIndexByTriadRowId[npcs[j].TriadRowId] = (ushort)j;
        }
        var rewardCards = rewardCardList.ToArray();
        BuildDropIndex(cards.Length, npcs, rewardCards, out var dropStart, out var dropNpcs);
        loaded = new TriadDataSet
        {
            Cards = cards,
            CardNames = names,
            CardItemIds = cardItemIds,
            CardIdsInOrder = SortedCardIds(cards),
            Npcs = npcs,
            NpcNames = npcNames.ToArray(),
            NpcUnlockQuests = unlockQuests.ToArray(),
            NpcUnlockQuestNames = unlockQuestNames.ToArray(),
            NpcDeckCards = deckCards.ToArray(),
            NpcRewardCards = rewardCards,
            CardDropStart = dropStart,
            CardDropNpcs = dropNpcs,
            RuleNames = ruleNames,
            NpcIndexByTriadRowId = npcIndexByTriadRowId,
            CardIdByItemId = cardIdByItemId
        };
        RunLog.Info($"Loaded Triple Triad data: {loaded.CardCount} cards, {npcs.Length} NPCs, {rewardCards.Length} NPC rewards.");
        failure = string.Empty;
        return true;
    }

    private static bool TryReadCards(out TriadCard[] cards, out string[] names, out string failure)
    {
        var residentSheet = Svc.Data.GetExcelSheet<TripleTriadCardResident>();
        var cardSheet = Svc.Data.GetExcelSheet<TripleTriadCard>();
        cards = Array.Empty<TriadCard>();
        names = Array.Empty<string>();
        if (residentSheet.Count != cardSheet.Count)
        {
            failure = $"The card tables disagree ({residentSheet.Count} stat rows, {cardSheet.Count} name rows); the plugin needs an update.";
            return false;
        }
        var maxRowId = 0u;
        for (var i = 0; i < residentSheet.Count; i++)
        {
            maxRowId = Math.Max(maxRowId, residentSheet.GetRowAt(i).RowId);
        }
        cards = new TriadCard[maxRowId + 1];
        names = new string[maxRowId + 1];
        for (var j = 0; j < residentSheet.Count; j++)
        {
            var resident = residentSheet.GetRowAt(j);
            var rowId = resident.RowId;
            names[rowId] = string.Empty;
            if (rowId != 0 && resident.Top != 0 && rowId <= ushort.MaxValue)
            {
                var stars = (byte)Math.Clamp(resident.TripleTriadCardRarity.RowId, 1u, 5u);
                var type = ((resident.TripleTriadCardType.RowId < 5) ? ((TriadCardType)resident.TripleTriadCardType.RowId) : TriadCardType.None);
                cards[rowId] = new TriadCard((ushort)rowId, resident.Top, resident.Right, resident.Bottom, resident.Left, type, stars, resident.Order, resident.UIPriority);
                names[rowId] = FixNameCasing(cardSheet.GetRowOrDefault(rowId)?.Name.ExtractText() ?? string.Empty);
            }
        }
        failure = string.Empty;
        return true;
    }

    private static Dictionary<uint, ushort> ReadCardItems(TriadCard[] cards, uint[] cardItemIds)
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        Dictionary<uint, ushort> cardIdByItemId = new();
        for (var i = 0; i < itemSheet.Count; i++)
        {
            var item = itemSheet.GetRowAt(i);
            if (item.ItemUICategory.RowId != TriadCardItemCategory)
            {
                continue;
            }
            var cardId = item.AdditionalData.RowId;
            if (cardId != 0 && cardId < cards.Length && cards[cardId].IsValid)
            {
                cardIdByItemId[item.RowId] = (ushort)cardId;
                if (cardItemIds[cardId] == 0)
                {
                    cardItemIds[cardId] = item.RowId;
                }
            }
        }
        return cardIdByItemId;
    }

    private static void ReadNpcs(TriadCard[] cards, Dictionary<uint, ushort> cardIdByItemId, List<TriadNpc> npcs, List<string> names, List<uint> unlockQuests, List<string> questNames, List<ushort> deckCards, List<ushort> rewardCards)
    {
        var triadSheet = Svc.Data.GetExcelSheet<TripleTriad>();
        var candidates = FindTriadENpcs(triadSheet);
        var levels = ReadNpcLevels(candidates);
        var eNpcByTriadRow = MapTriadRowsToENpcs(candidates, levels);
        var eNpcSheet = Svc.Data.GetExcelSheet<ENpcResident>();
        var mapSheet = Svc.Data.GetExcelSheet<Map>();
        var territorySheet = Svc.Data.GetExcelSheet<TerritoryType>();
        var questSheet = Svc.Data.GetExcelSheet<Quest>();
        for (var i = 0; i < triadSheet.Count; i++)
        {
            var triad = triadSheet.GetRowAt(i);
            if (triad.RowId == 0 || !eNpcByTriadRow.TryGetValue(triad.RowId, out var eNpcId))
            {
                continue;
            }
            var npcLevel = levels[eNpcId];
            var deckStart = deckCards.Count;
            var fixedCount = AppendDeck(triad.TripleTriadCardFixed, cards, deckCards);
            var variableCount = AppendDeck(triad.TripleTriadCardVariable, cards, deckCards);
            if (fixedCount == 0 && variableCount == 0)
            {
                deckCards.RemoveRange(deckStart, deckCards.Count - deckStart);
                continue;
            }
            var rewardStart = rewardCards.Count;
            for (var j = 0; j < triad.ItemPossibleReward.Count; j++)
            {
                var itemId = triad.ItemPossibleReward[j].RowId;
                if (itemId != 0 && cardIdByItemId.TryGetValue(itemId, out var rewardCardId) && !ContainsCard(rewardCards, rewardStart, rewardCardId))
                {
                    rewardCards.Add(rewardCardId);
                }
            }
            ushort ruleMask = 0;
            for (var k = 0; k < triad.TripleTriadRule.Count; k++)
            {
                var ruleId = triad.TripleTriadRule[k].RowId;
                if (ruleId > 0 && ruleId < TriadRuleIds.Count)
                {
                    ruleMask = TriadRuleIds.With(ruleMask, (TriadRuleId)ruleId);
                }
            }
            for (var l = 0; l < TriadNpc.UnlockQuestSlots; l++)
            {
                var questId = ((l < triad.PreviousQuest.Count) ? triad.PreviousQuest[l].RowId : 0u);
                unlockQuests.Add(questId);
                questNames.Add((questId == 0) ? string.Empty : (questSheet.GetRowOrDefault(questId)?.Name.ExtractText().Trim() ?? string.Empty));
            }
            var map = mapSheet.GetRowOrDefault(npcLevel.MapId);
            var mapCoordinates = ((!map.HasValue) ? Vector2.Zero : new Vector2(ToMapCoordinate(npcLevel.Position.X, map.Value.SizeFactor, map.Value.OffsetX), ToMapCoordinate(npcLevel.Position.Z, map.Value.SizeFactor, map.Value.OffsetY)));
            var expansion = (byte)(territorySheet.GetRowOrDefault(npcLevel.TerritoryId)?.ExVersion.RowId ?? 0);
            npcs.Add(new TriadNpc(triad.RowId, eNpcId, npcLevel.TerritoryId, npcLevel.MapId, npcLevel.Position, mapCoordinates, triad.PreviousQuestJoin == PreviousQuestJoinAny, triad.Fee, ruleMask, triad.UsesRegionalRules, expansion, deckStart, (byte)fixedCount, (byte)variableCount, rewardStart, (byte)(rewardCards.Count - rewardStart)));
            names.Add(FixNameCasing(eNpcSheet.GetRowOrDefault(eNpcId)?.Singular.ExtractText() ?? $"NPC {eNpcId}"));
        }
    }

    private static int AppendDeck(Collection<RowRef<TripleTriadCard>> slots, TriadCard[] cards, List<ushort> deckCards)
    {
        var count = 0;
        for (var i = 0; i < TriadNpc.DeckSlots; i++)
        {
            var cardId = ((i < slots.Count) ? slots[i].RowId : 0u);
            if (cardId != 0 && cardId < cards.Length && cards[cardId].IsValid)
            {
                deckCards.Add((ushort)cardId);
                count++;
            }
        }
        for (var j = count; j < TriadNpc.DeckSlots; j++)
        {
            deckCards.Add(0);
        }
        return count;
    }

    private static bool ContainsCard(List<ushort> cards, int start, ushort cardId)
    {
        for (var i = start; i < cards.Count; i++)
        {
            if (cards[i] == cardId)
            {
                return true;
            }
        }
        return false;
    }

    private static List<TriadENpc> FindTriadENpcs(ExcelSheet<TripleTriad> triadSheet)
    {
        HashSet<uint> triadRowIds = new(triadSheet.Count);
        for (var i = 0; i < triadSheet.Count; i++)
        {
            var rowId = triadSheet.GetRowAt(i).RowId;
            if (rowId != 0)
            {
                triadRowIds.Add(rowId);
            }
        }
        var eNpcSheet = Svc.Data.GetExcelSheet<ENpcBase>();
        List<TriadENpc> candidates = new();
        for (var j = 0; j < eNpcSheet.Count; j++)
        {
            var eNpc = eNpcSheet.GetRowAt(j);
            for (var k = 0; k < eNpc.ENpcData.Count; k++)
            {
                var triadRowId = eNpc.ENpcData[k].RowId;
                if (triadRowIds.Contains(triadRowId))
                {
                    candidates.Add(new TriadENpc(eNpc.RowId, triadRowId));
                    break;
                }
            }
        }
        return candidates;
    }

    private static Dictionary<uint, uint> MapTriadRowsToENpcs(List<TriadENpc> candidates, Dictionary<uint, NpcLevel> levels)
    {
        Dictionary<uint, uint> eNpcByTriadRow = new();
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (levels.TryGetValue(candidate.ENpcId, out var level) && (!eNpcByTriadRow.TryGetValue(candidate.TriadRowId, out var existingENpcId) || IsBetterLocation(level, levels[existingENpcId])))
            {
                eNpcByTriadRow[candidate.TriadRowId] = candidate.ENpcId;
            }
        }
        return eNpcByTriadRow;
    }

    private static Dictionary<uint, NpcLevel> ReadNpcLevels(List<TriadENpc> candidates)
    {
        HashSet<uint> eNpcIds = new(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            eNpcIds.Add(candidates[i].ENpcId);
        }
        var levelSheet = Svc.Data.GetExcelSheet<Level>();
        var mapSheet = Svc.Data.GetExcelSheet<Map>();
        var territorySheet = Svc.Data.GetExcelSheet<TerritoryType>();
        Dictionary<uint, NpcLevel> levels = new(eNpcIds.Count);
        for (var j = 0; j < levelSheet.Count; j++)
        {
            var level = levelSheet.GetRowAt(j);
            if (level.Type == LevelTypeNpc && eNpcIds.Contains(level.Object.RowId) && TryReadNpcLocation(level, mapSheet, territorySheet, out var location) && (!levels.TryGetValue(level.Object.RowId, out var existing) || IsBetterLocation(location, existing)))
            {
                levels[level.Object.RowId] = location;
            }
        }
        return levels;
    }

    private static bool TryReadNpcLocation(Level level, ExcelSheet<Map> mapSheet, ExcelSheet<TerritoryType> territorySheet, out NpcLevel location)
    {
        location = default;
        if (level.Map.RowId == 0 || mapSheet.GetRowOrDefault(level.Map.RowId) is not { } map || map.SizeFactor is 0 || !float.IsFinite(level.X) || !float.IsFinite(level.Y) || !float.IsFinite(level.Z))
        {
            return false;
        }
        var territoryId = ((level.Territory.RowId != 0) ? level.Territory.RowId : map.TerritoryType.RowId);
        if (territoryId == 0 || territorySheet.GetRowOrDefault(territoryId) is not { IsInUse: true })
        {
            return false;
        }
        location = new NpcLevel(new Vector3(level.X, level.Y, level.Z), level.Map.RowId, territoryId, map.IsEvent);
        return true;
    }

    private static bool IsBetterLocation(NpcLevel candidate, NpcLevel current) => current.OnEventMap && !candidate.OnEventMap;

    private static void BuildDropIndex(int cardSlots, TriadNpc[] npcs, ushort[] rewards, out int[] dropStart, out ushort[] dropNpcs)
    {
        var dropCounts = new int[cardSlots + 1];
        for (var i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            for (var j = 0; j < npc.RewardCount; j++)
            {
                dropCounts[rewards[npc.RewardStart + j]]++;
            }
        }
        dropStart = new int[cardSlots + 1];
        for (var k = 1; k <= cardSlots; k++)
        {
            dropStart[k] = dropStart[k - 1] + dropCounts[k - 1];
        }
        dropNpcs = new ushort[dropStart[cardSlots]];
        var filled = new int[cardSlots];
        for (var l = 0; l < npcs.Length; l++)
        {
            var npc = npcs[l];
            for (var m = 0; m < npc.RewardCount; m++)
            {
                var cardId = rewards[npc.RewardStart + m];
                dropNpcs[dropStart[cardId] + filled[cardId]] = (ushort)l;
                filled[cardId]++;
            }
        }
    }

    private static ushort[] SortedCardIds(TriadCard[] cards)
    {
        List<ushort> cardIds = new(cards.Length);
        for (var i = 1; i < cards.Length; i++)
        {
            if (cards[i].IsValid)
            {
                cardIds.Add((ushort)i);
            }
        }
        cardIds.Sort((ushort left, ushort right) =>
        {
            var groupCompare = cards[left].Group.CompareTo(cards[right].Group);
            return (groupCompare != 0) ? groupCompare : cards[left].Order.CompareTo(cards[right].Order);
        });
        return cardIds.ToArray();
    }

    private static float ToMapCoordinate(float world, ushort sizeFactor, short offset)
    {
        if (sizeFactor != 0)
        {
            return 0.02f * (world + (float)offset) + 2048f / (float)(int)sizeFactor + 1f;
        }
        return 0f;
    }

    private static string FixNameCasing(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        var words = text.Split(' ');
        var changed = false;
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length > 2 && Array.IndexOf(casingExclusions, word) < 0 && word[1] != '\'' && char.IsLower(word, 0))
            {
                words[i] = string.Concat(char.ToUpperInvariant(word[0]).ToString(), word.Substring(1));
                changed = true;
            }
        }
        if (!changed)
        {
            return text;
        }
        return string.Join(' ', words);
    }
}
