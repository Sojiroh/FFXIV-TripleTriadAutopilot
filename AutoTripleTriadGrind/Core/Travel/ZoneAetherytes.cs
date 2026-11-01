using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class ZoneAetherytes
{
    private enum StopFilter : byte
    {
        Any,
        Attuned,
        Usable
    }

    private readonly record struct AethernetNode(ZoneAetheryte Stop, uint PrimaryId, bool Visible);

    private sealed record AetheryteIndex(Dictionary<uint, Aetheryte[]> RowsByTerritory, Dictionary<byte, uint> PrimaryByGroup);

    private const byte StandardFieldUse = 1;

    private const float AethernetOverheadMeters = 80f;

    private static readonly Dictionary<uint, ZoneAetheryte[]> byTerritory = new();

    private static readonly Dictionary<uint, uint[]> attunableIdsByTerritory = new();

    private static readonly Dictionary<uint, ZoneGateway?> gatewayByTerritory = new();

    private static readonly Dictionary<uint, AethernetNode[]> aethernetByTerritory = new();

    private static AetheryteIndex? sheetIndex;

    private static AetheryteIndex SheetIndex => sheetIndex ?? (sheetIndex = BuildSheetIndex());

    public static bool TryFindNearest(uint territoryId, Vector3 target, out ZoneAetheryte nearest)
    {
        var aetherytes = InTerritory(territoryId);
        nearest = default;
        var bestDistanceSquared = float.MaxValue;
        for (var i = 0; i < aetherytes.Length; i++)
        {
            var aetheryte = aetherytes[i];
            if (IsAttuned(aetheryte.Id))
            {
                var distanceSquared = Vector3.DistanceSquared(aetheryte.Position, target);
                if (!(distanceSquared >= bestDistanceSquared))
                {
                    bestDistanceSquared = distanceSquared;
                    nearest = aetheryte;
                }
            }
        }
        return bestDistanceSquared < float.MaxValue;
    }

    public static ReadOnlyMemory<ZoneAetheryte> TeleportableIn(uint territoryId)
    {
        return InTerritory(territoryId);
    }

    public static uint[] AttunableIdsIn(uint territoryId)
    {
        if (attunableIdsByTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var ids = ResolveAttunableIds(territoryId);
        attunableIdsByTerritory[territoryId] = ids;
        return ids;
    }

    public static bool TryFindGateway(uint territoryId, out ZoneGateway gateway)
    {
        if (!gatewayByTerritory.TryGetValue(territoryId, out var resolved))
        {
            resolved = ResolveGateway(territoryId);
            gatewayByTerritory[territoryId] = resolved;
        }
        gateway = resolved.GetValueOrDefault();
        return resolved.HasValue;
    }

    public static bool TryFindEntryNode(uint territoryId, Vector3 destination, out ZoneAetheryte entry)
    {
        var nodes = AethernetIn(territoryId);
        var index = ClosestNode(nodes, destination, StopFilter.Usable);
        if (index < 0)
        {
            index = ClosestNode(nodes, destination, StopFilter.Attuned);
        }
        entry = ((index < 0) ? default(ZoneAetheryte) : nodes[index].Stop);
        return index >= 0;
    }

    public static bool HasAethernetStops(uint territoryId)
    {
        return AethernetIn(territoryId).Length != 0;
    }

    public static bool TryFindAethernetShortcut(uint territoryId, Vector3 from, Vector3 to, out AethernetShortcut shortcut)
    {
        shortcut = default;
        var nodes = AethernetIn(territoryId);
        var fromIndex = ClosestNode(nodes, from, StopFilter.Any);
        if (fromIndex < 0 || nodes[fromIndex].PrimaryId == 0 || !IsUsable(nodes[fromIndex]))
        {
            return false;
        }
        var fromNode = nodes[fromIndex];
        var toIndex = -1;
        var bestToDistance = float.MaxValue;
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (i != fromIndex && node.PrimaryId == fromNode.PrimaryId && IsUsable(node))
            {
                var distance = GroundDistance.Between(node.Stop.Position, to);
                if (!(distance >= bestToDistance))
                {
                    bestToDistance = distance;
                    toIndex = i;
                }
            }
        }
        if (toIndex < 0)
        {
            return false;
        }
        var walkDistance = GroundDistance.Between(from, to);
        var aethernetDistance = GroundDistance.Between(from, fromNode.Stop.Position) + bestToDistance + AethernetOverheadMeters;
        if (aethernetDistance >= walkDistance)
        {
            return false;
        }
        shortcut = new AethernetShortcut(fromNode.Stop, nodes[toIndex].Stop, walkDistance - aethernetDistance);
        return true;
    }

    public unsafe static bool IsAttuned(uint aetheryteId)
    {
        var uiState = UIState.Instance();
        if (uiState != null)
        {
            return uiState->IsAetheryteUnlocked(aetheryteId);
        }
        return false;
    }

    private static bool IsUsable(AethernetNode node)
    {
        if (node.Visible)
        {
            return IsAttuned(node.Stop.Id);
        }
        return false;
    }

    private static bool Passes(AethernetNode node, StopFilter filter)
    {
        return filter switch
        {
            StopFilter.Usable => IsUsable(node),
            StopFilter.Attuned => IsAttuned(node.Stop.Id),
            _ => true,
        };
    }

    private static int ClosestNode(AethernetNode[] nodes, Vector3 position, StopFilter filter)
    {
        var closestIndex = -1;
        var bestDistanceSquared = float.MaxValue;
        for (var i = 0; i < nodes.Length; i++)
        {
            if (Passes(nodes[i], filter))
            {
                var distanceSquared = GroundDistance.SquaredBetween(nodes[i].Stop.Position, position);
                if (!(distanceSquared >= bestDistanceSquared))
                {
                    bestDistanceSquared = distanceSquared;
                    closestIndex = i;
                }
            }
        }
        return closestIndex;
    }

    private static ZoneAetheryte[] InTerritory(uint territoryId)
    {
        if (byTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var aetherytes = ResolveTeleportableAetherytes(territoryId);
        byTerritory[territoryId] = aetherytes;
        return aetherytes;
    }

    private static AethernetNode[] AethernetIn(uint territoryId)
    {
        if (aethernetByTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var nodes = ResolveAethernet(territoryId);
        aethernetByTerritory[territoryId] = nodes;
        return nodes;
    }

    private static ZoneGateway? ResolveGateway(uint territoryId)
    {
        if (AttunableIdsIn(territoryId).Length != 0)
        {
            return null;
        }
        if (Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryId) is not { } territory)
        {
            return null;
        }
        var aetheryteId = ((territory.TerritoryIntendedUse.RowId == StandardFieldUse) ? territory.Aetheryte.RowId : DistrictAetheryte(territoryId));
        if (aetheryteId == 0)
        {
            return null;
        }
        if (Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(aetheryteId) is not { } aetheryte || !aetheryte.IsAetheryte)
        {
            return null;
        }
        var aetheryteTerritoryId = aetheryte.Territory.RowId;
        if (aetheryteTerritoryId == 0 || aetheryteTerritoryId == territoryId || !TryResolvePosition(aetheryte, out var position))
        {
            return null;
        }
        return new ZoneGateway(aetheryteId, aetheryteTerritoryId, ResolveName(aetheryte), position);
    }

    private static uint DistrictAetheryte(uint territoryId)
    {
        var nodes = AethernetIn(territoryId);
        for (var i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Visible && nodes[i].PrimaryId != 0)
            {
                return nodes[i].PrimaryId;
            }
        }
        return 0u;
    }

    private static uint[] ResolveAttunableIds(uint territoryId)
    {
        var rows = RowsIn(territoryId);
        var ids = new List<uint>(rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i].IsAetheryte)
            {
                ids.Add(rows[i].RowId);
            }
        }
        return ids.ToArray();
    }

    private static ZoneAetheryte[] ResolveTeleportableAetherytes(uint territoryId)
    {
        var sheet = Svc.Data.GetExcelSheet<Aetheryte>();
        var attunableIds = AttunableIdsIn(territoryId);
        var teleportable = new List<ZoneAetheryte>(attunableIds.Length);
        for (var i = 0; i < attunableIds.Length; i++)
        {
            if (sheet.GetRowOrDefault(attunableIds[i]) is { } aetheryte && TryResolvePosition(aetheryte, out var position))
            {
                teleportable.Add(new ZoneAetheryte(aetheryte.RowId, ResolveName(aetheryte), position));
            }
        }
        return teleportable.ToArray();
    }

    private static AethernetNode[] ResolveAethernet(uint territoryId)
    {
        var rows = RowsIn(territoryId);
        var nodes = new List<AethernetNode>(rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (TryResolvePosition(row, out var position))
            {
                nodes.Add(new AethernetNode(new ZoneAetheryte(row.RowId, ResolveName(row), position), PrimaryOf(row), !row.Invisible));
            }
        }
        return nodes.ToArray();
    }

    private static uint PrimaryOf(Aetheryte row)
    {
        if (row.IsAetheryte)
        {
            return row.RowId;
        }
        if (!SheetIndex.PrimaryByGroup.TryGetValue(row.AethernetGroup, out var primaryId))
        {
            return 0u;
        }
        return primaryId;
    }

    private static ReadOnlySpan<Aetheryte> RowsIn(uint territoryId)
    {
        return SheetIndex.RowsByTerritory.TryGetValue(territoryId, out var rows) ? rows : Array.Empty<Aetheryte>();
    }

    private static AetheryteIndex BuildSheetIndex()
    {
        var sheet = Svc.Data.GetExcelSheet<Aetheryte>();
        var rowsByTerritory = new Dictionary<uint, List<Aetheryte>>();
        var primaryByGroup = new Dictionary<byte, uint>();
        for (var i = 0; i < sheet.Count; i++)
        {
            var row = sheet.GetRowAt(i);
            if (row.IsAetheryte)
            {
                primaryByGroup.TryAdd(row.AethernetGroup, row.RowId);
            }
            if (!rowsByTerritory.TryGetValue(row.Territory.RowId, out var territoryRows))
            {
                territoryRows = new List<Aetheryte>();
                rowsByTerritory[row.Territory.RowId] = territoryRows;
            }
            territoryRows.Add(row);
        }
        var arraysByTerritory = new Dictionary<uint, Aetheryte[]>(rowsByTerritory.Count);
        foreach (var (territoryId, rows) in rowsByTerritory)
        {
            arraysByTerritory[territoryId] = rows.ToArray();
        }
        return new AetheryteIndex(arraysByTerritory, primaryByGroup);
    }

    private static string ResolveName(Aetheryte row)
    {
        var placeName = (row.IsAetheryte ? row.PlaceName : row.AethernetName);
        var name = placeName.ValueNullable?.Name.ExtractText();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }
        return $"aetheryte #{row.RowId}";
    }

    private static bool TryResolvePosition(Aetheryte row, out Vector3 position)
    {
        try
        {
            position = ECommons.GameHelpers.Map.AetherytePosition(row);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, $"Could not resolve a position for aetheryte {row.RowId}; skipping it as a travel target");
            position = default;
            return false;
        }
        if (float.IsFinite(position.X) && float.IsFinite(position.Y))
        {
            return float.IsFinite(position.Z);
        }
        return false;
    }
}
