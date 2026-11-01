using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Logic;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class TriadCardMatcher
{
    private static readonly string[] texturePrefixes = new string[2] { "088000/088", "087000/087" };

    private const string LegacyTexturePrefix = "082000/082";

    private static TriadDataSet? builtFor;

    private static Dictionary<long, List<ushort>> byStatsTypeStars = new();

    private static Dictionary<long, List<ushort>> byStats = new();

    public static ushort Match(byte up, byte addonLeft, byte down, byte addonRight, byte type, byte rarity, string? texturePath)
    {
        EnsureBuilt();
        var right = addonLeft;
        var left = addonRight;
        var stars = (byte)Math.Clamp((int)rarity, 1, 5);
        if (byStatsTypeStars.TryGetValue((long)((ulong)StatsKey(up, right, down, left) | ((ulong)type << 32) | ((ulong)stars << 40)), out var exactMatches))
        {
            if (exactMatches.Count == 1)
            {
                return exactMatches[0];
            }
            var textureCard = FromTexture(texturePath);
            if (exactMatches.Contains(textureCard))
            {
                return textureCard;
            }
        }
        if (byStats.TryGetValue(StatsKey(up, right, down, left), out var statMatches) && statMatches.Count == 1)
        {
            return statMatches[0];
        }
        var fallbackCard = FromTexture(texturePath);
        if (!TriadData.Set.IsCard(fallbackCard))
        {
            return TriadCards.None;
        }
        return fallbackCard;
    }

    public static ushort FromTexture(string? texturePath)
    {
        if (string.IsNullOrEmpty(texturePath) || !texturePath.EndsWith(".tex", StringComparison.OrdinalIgnoreCase))
        {
            return TriadCards.None;
        }
        var legacyIndex = texturePath.IndexOf(LegacyTexturePrefix, StringComparison.Ordinal);
        if (legacyIndex > 0 && TryReadId(texturePath, legacyIndex + LegacyTexturePrefix.Length, out var legacyId))
        {
            var legacyCardId = (legacyId >= 500) ? (legacyId - 500) : ((legacyId >= 100) ? (legacyId - 100) : (-1));
            if (legacyCardId < 0)
            {
                return TriadCards.None;
            }
            return (ushort)legacyCardId;
        }
        for (var i = 0; i < texturePrefixes.Length; i++)
        {
            var prefixIndex = texturePath.IndexOf(texturePrefixes[i], StringComparison.Ordinal);
            if (prefixIndex > 0 && TryReadId(texturePath, prefixIndex + texturePrefixes[i].Length, out var cardId))
            {
                return (ushort)cardId;
            }
        }
        return TriadCards.None;
    }

    private static bool TryReadId(string path, int start, out int id)
    {
        id = -1;
        return start + 3 <= path.Length && int.TryParse(path.AsSpan(start, 3), out id);
    }

    private static long StatsKey(byte up, byte right, byte down, byte left)
    {
        return (long)(up | ((ulong)right << 8) | ((ulong)down << 16) | ((ulong)left << 24));
    }

    private static void EnsureBuilt()
    {
        var set = TriadData.Set;
        if (builtFor == set)
        {
            return;
        }
        Dictionary<long, List<ushort>> statsTypeStarsMap = new();
        Dictionary<long, List<ushort>> statsMap = new();
        var cardIdsInOrder = set.CardIdsInOrder;
        for (var i = 0; i < cardIdsInOrder.Length; i++)
        {
            ref var card = ref set.Cards[cardIdsInOrder[i]];
            var statsKey = StatsKey(card.Up, card.Right, card.Down, card.Left);
            Add(statsTypeStarsMap, (long)((ulong)statsKey | ((ulong)card.Type << 32) | ((ulong)card.Stars << 40)), card.Id);
            Add(statsMap, statsKey, card.Id);
        }
        byStatsTypeStars = statsTypeStarsMap;
        byStats = statsMap;
        builtFor = set;
    }

    private static void Add(Dictionary<long, List<ushort>> map, long key, ushort cardId)
    {
        if (!map.TryGetValue(key, out var cardIds))
        {
            cardIds = new List<ushort>(1);
            map[key] = cardIds;
        }
        cardIds.Add(cardId);
    }
}
