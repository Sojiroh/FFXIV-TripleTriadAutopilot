using System;
using System.Globalization;

namespace AutoTripleTriadGrind.Windows;

internal static class NumberText
{
    private static string[] numbers = Array.Empty<string>();

    public static string Of(int value)
    {
        if (value >= numbers.Length)
        {
            Grow(value + 1);
        }
        return numbers[value];
    }

    private static void Grow(int count)
    {
        var grown = new string[Math.Max(count, numbers.Length * 2)];
        Array.Copy(numbers, grown, numbers.Length);
        for (var i = numbers.Length; i < grown.Length; i++)
        {
            grown[i] = i.ToString(CultureInfo.InvariantCulture);
        }
        numbers = grown;
    }
}
