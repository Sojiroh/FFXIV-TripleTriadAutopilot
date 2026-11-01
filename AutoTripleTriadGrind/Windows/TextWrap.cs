using System;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace AutoTripleTriadGrind.Windows;

internal static class TextWrap
{
    public static string Hard(string text, float width)
    {
        if (width <= 0f || (ImGui.CalcTextSize(text).X <= width && text.IndexOf('\n') < 0))
        {
            return text;
        }
        var wrapped = new StringBuilder(text.Length + text.Length / 32);
        var lineStart = 0;
        while (lineStart <= text.Length)
        {
            var lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }
            var line = text.AsSpan(lineStart, lineEnd - lineStart).TrimEnd('\r');
            AppendWrapped(wrapped, line, width);
            if (lineEnd >= text.Length)
            {
                break;
            }
            wrapped.Append('\n');
            lineStart = lineEnd + 1;
        }
        return wrapped.ToString();
    }

    private static void AppendWrapped(StringBuilder builder, ReadOnlySpan<char> line, float width)
    {
        while (ImGui.CalcTextSize(line).X > width)
        {
            var breakAt = FitLength(line, width);
            var lastSpace = line.Slice(0, breakAt).LastIndexOf(' ');
            if (lastSpace > 0 && breakAt < line.Length)
            {
                breakAt = lastSpace;
            }
            builder.Append(line.Slice(0, breakAt)).Append('\n');
            line = line.Slice(breakAt).TrimStart(' ');
        }
        builder.Append(line);
    }

    private static int FitLength(ReadOnlySpan<char> line, float width)
    {
        var low = 1;
        var high = line.Length;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            if (ImGui.CalcTextSize(line.Slice(0, mid)).X <= width)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }
        return low;
    }
}
