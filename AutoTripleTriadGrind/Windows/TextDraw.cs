using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoTripleTriadGrind.Windows;

internal static class TextDraw
{
    private readonly record struct TruncatedText(string Source, float MaxWidth, float FontSize, string Result);

    public const string Separator = "  ·  ";

    private const string Ellipsis = "…";

    private const int TruncateCacheSize = 16;

    private static readonly TruncatedText[] truncateCache = new TruncatedText[TruncateCacheSize];

    private static int truncateCacheNext;

    public static string Upper(string text)
    {
        return Loc.Upper(text);
    }

    public static Vector2 Measure(string text)
    {
        return ImGui.CalcTextSize(text);
    }

    public static Vector2 MeasureWrapped(string text, float wrapWidth)
    {
        return ImGui.CalcTextSize(text, hideTextAfterDoubleHash: false, wrapWidth);
    }

    public static void At(string text, Vector2 pos, Vector4 color)
    {
        ImGui.GetWindowDrawList().AddText(pos, Paint.Col(color), text);
    }

    public static void Right(string text, float rightX, float y, Vector4 color)
    {
        At(text, new Vector2(rightX - Measure(text).X, y), color);
    }

    public static void Center(string text, float centerX, float y, Vector4 color)
    {
        At(text, new Vector2(centerX - Measure(text).X * 0.5f, y), color);
    }

    public static void Middle(string text, Vector2 min, Vector2 max, Vector4 color)
    {
        var textSize = Measure(text);
        At(text, new Vector2((min.X + max.X - textSize.X) * 0.5f, (min.Y + max.Y - textSize.Y) * 0.5f), color);
    }

    public static void Wrapped(string text, Vector2 pos, float wrapWidth, Vector4 color)
    {
        ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, Paint.Col(color), text, wrapWidth);
    }

    public static void Hint(string text)
    {
        At(text, ImGui.GetCursorScreenPos(), Styling.TextMuted);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()));
    }

    public static Vector2 IconSize(FontAwesomeIcon icon)
    {
        using (Fonts.PushIcon())
        {
            return Measure(icon.ToIconString());
        }
    }

    public static void Icon(FontAwesomeIcon icon, Vector2 pos, Vector4 color)
    {
        using (Fonts.PushIcon())
        {
            At(icon.ToIconString(), pos, color);
        }
    }

    public static void IconCentered(FontAwesomeIcon icon, Vector2 center, Vector4 color)
    {
        using (Fonts.PushIcon())
        {
            var glyph = icon.ToIconString();
            var glyphSize = Measure(glyph);
            At(glyph, center - glyphSize * 0.5f, color);
        }
    }

    public static void IconCentered(FontAwesomeIcon icon, Vector2 center, Vector4 color, float sizeScale)
    {
        using (Fonts.PushIcon())
        {
            var glyph = icon.ToIconString();
            var glyphSize = Measure(glyph) * sizeScale;
            ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), ImGui.GetFontSize() * sizeScale, center - glyphSize * 0.5f, Paint.Col(color), glyph);
        }
    }

    public static void IconRight(FontAwesomeIcon icon, float rightX, float centerY, Vector4 color)
    {
        using (Fonts.PushIcon())
        {
            var glyph = icon.ToIconString();
            var glyphSize = Measure(glyph);
            At(glyph, new Vector2(rightX - glyphSize.X, centerY - glyphSize.Y * 0.5f), color);
        }
    }

    public static string Truncate(string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
        {
            return string.Empty;
        }
        if (Measure(text).X <= maxWidth)
        {
            return text;
        }
        var fontSize = ImGui.GetFontSize();
        for (var i = 0; i < truncateCache.Length; i++)
        {
            var entry = truncateCache[i];
            if ((object)entry.Source == text && entry.MaxWidth == maxWidth && entry.FontSize == fontSize)
            {
                return entry.Result;
            }
        }
        var truncated = Cut(text, maxWidth);
        truncateCache[truncateCacheNext] = new TruncatedText(text, maxWidth, fontSize, truncated);
        truncateCacheNext = (truncateCacheNext + 1) % TruncateCacheSize;
        return truncated;
    }

    private static string Cut(string text, float maxWidth)
    {
        var available = maxWidth - Measure(Ellipsis).X;
        if (available <= 0f)
        {
            return Ellipsis;
        }
        var low = 1;
        var high = text.Length - 1;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (ImGui.CalcTextSize(text.AsSpan(0, mid)).X <= available)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }
        return string.Concat(text.AsSpan(0, low), Ellipsis);
    }

    public static void Trailing(string text, float x, float rightX, float y, Vector4 color)
    {
        var separatorWidth = Measure(Separator).X;
        if (string.IsNullOrWhiteSpace(text) || rightX - x <= separatorWidth)
        {
            return;
        }
        At(Separator, new Vector2(x, y), color);
        At(Truncate(text, rightX - x - separatorWidth), new Vector2(x + separatorWidth, y), color);
    }

    public static void SmallCaps(string label, Vector2 pos, Vector4 color)
    {
        using (Fonts.PushCaption())
        {
            At(Upper(label), pos, color);
        }
    }

    public static Vector2 SmallCapsSize(string label)
    {
        using (Fonts.PushCaption())
        {
            return Measure(Upper(label));
        }
    }

    public static void SectionTitle(string label, Vector2 pos, Vector4 color)
    {
        using (Fonts.PushHeadline())
        {
            At(label, pos, color);
        }
    }

    public static Vector2 SectionTitleSize(string label)
    {
        using (Fonts.PushHeadline())
        {
            return Measure(label);
        }
    }

    public static float LineHeight()
    {
        return ImGui.GetTextLineHeight();
    }
}
