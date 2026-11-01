using System;
using AutoTripleTriadGrind.Core;

namespace AutoTripleTriadGrind.Windows;

internal static class Formatting
{
    public static string Exp(long exp)
    {
        if (exp >= 1000000)
        {
            return $"{exp / 1000000.0:0.0}M";
        }
        if (exp >= 1000)
        {
            return $"{exp / 1000.0:0.0}K";
        }
        return exp.ToString();
    }

    public static string Elapsed(TimeSpan t)
    {
        if (t.TotalHours >= 1.0)
        {
            return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
        }
        return $"{t.Minutes}m {t.Seconds:D2}s";
    }

    public static string Time(float secs)
    {
        if (secs <= 0f)
        {
            return "--:--";
        }
        var totalSeconds = (int)secs;
        return $"{totalSeconds / TimeUnits.SecondsPerMinute}:{totalSeconds % TimeUnits.SecondsPerMinute:D2}";
    }
}
