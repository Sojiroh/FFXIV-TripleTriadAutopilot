using System.Collections.Generic;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoTripleTriadGrind.Core.Travel;

internal static class TerritoryNames
{
    private static readonly Dictionary<uint, string> names = new();

    public static string Of(uint territoryId)
    {
        if (names.TryGetValue(territoryId, out var cachedName))
        {
            return cachedName;
        }
        var placeName = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryId)?.PlaceName.ValueNullable?.Name.ExtractText();
        var name = (string.IsNullOrWhiteSpace(placeName) ? $"territory {territoryId}" : placeName);
        names[territoryId] = name;
        return name;
    }
}
