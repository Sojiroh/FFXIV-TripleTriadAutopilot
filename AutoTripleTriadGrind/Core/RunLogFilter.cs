using System;

namespace AutoTripleTriadGrind.Core;

public readonly record struct RunLogFilter(int LevelMask, string Search, string? Source)
{
    public bool IsNarrowed => LevelMask != AllLevels || Search.Length > 0 || Source is not null;

    public const int AllLevels = 31;

    public static readonly RunLogFilter Everything = new RunLogFilter(AllLevels, string.Empty, null);

    public bool Shows(RunLogLevel level)
    {
        return (LevelMask & (1 << (int)level)) != 0;
    }

    public bool Matches(in RunLogLine line)
    {
        if (!Shows(line.Level))
        {
            return false;
        }
        if (Source != null && !string.Equals(line.Source, Source, StringComparison.Ordinal))
        {
            return false;
        }
        if (Search.Length == 0)
        {
            return true;
        }
        if (!line.Message.Contains(Search, StringComparison.OrdinalIgnoreCase) && !line.Source.Contains(Search, StringComparison.OrdinalIgnoreCase))
        {
            if (line.Detail != null)
            {
                return line.Detail.Contains(Search, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
        return true;
    }
}
