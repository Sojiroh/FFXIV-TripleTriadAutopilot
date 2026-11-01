using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AutoTripleTriadGrind.Core.Localization;

internal sealed class StringCatalog
{
    private const int FirstNonAsciiCodepoint = 128;

    public static readonly StringCatalog Empty = new StringCatalog(new Dictionary<string, string>(0, StringComparer.Ordinal));

    private readonly Dictionary<string, string> entries;

    public int Count => entries.Count;

    private StringCatalog(Dictionary<string, string> entries)
    {
        this.entries = entries;
    }

    public bool TryGet(string key, out string value)
    {
        return entries.TryGetValue(key, out value!);
    }

    public bool Contains(string key)
    {
        return entries.ContainsKey(key);
    }

    public static StringCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            return Empty;
        }
        var target = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            Flatten(JObject.Parse(File.ReadAllText(path)), null, target);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to load language catalog '" + path + "'");
            return Empty;
        }
        return new StringCatalog(target);
    }

    public static ushort[] ScanGlyphRanges(string path)
    {
        if (!File.Exists(path))
        {
            return new ushort[1];
        }
        string fileText;
        try
        {
            fileText = File.ReadAllText(path);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to scan glyphs from '" + path + "'");
            return new ushort[1];
        }
        var present = new bool[GlyphRanges.CodepointCount];
        for (int i = 0; i < fileText.Length; i++)
        {
            var c = fileText[i];
            if (c >= FirstNonAsciiCodepoint && !char.IsSurrogate(c))
            {
                present[(uint)c] = true;
            }
        }
        var ranges = new List<ushort>();
        var rangeStart = -1;
        for (int j = FirstNonAsciiCodepoint; j <= 65535; j++)
        {
            if (present[j])
            {
                if (rangeStart < 0)
                {
                    rangeStart = j;
                }
            }
            else if (rangeStart >= 0)
            {
                ranges.Add((ushort)rangeStart);
                ranges.Add((ushort)(j - 1));
                rangeStart = -1;
            }
        }
        if (rangeStart >= 0)
        {
            ranges.Add((ushort)rangeStart);
            ranges.Add(ushort.MaxValue);
        }
        ranges.Add(0);
        return ranges.ToArray();
    }

    private static void Flatten(JObject node, string? prefix, Dictionary<string, string> target)
    {
        foreach (var property in node.Properties())
        {
            var key = ((prefix == null) ? property.Name : (prefix + "." + property.Name));
            if (property.Value is JObject child)
            {
                Flatten(child, key, target);
            }
            else
            {
                target[key] = property.Value.ToString();
            }
        }
    }
}
