using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core;

public static class RunLog
{
    public const int Capacity = 3000;

    public const int LevelCount = 5;

    private const string TimeFormat = "HH:mm:ss.fff";

    private static readonly object gate = new object();

    private static readonly RunLogLine[] lines = new RunLogLine[Capacity];

    private static readonly Dictionary<string, string> sourceNames = new Dictionary<string, string>(StringComparer.Ordinal);

    private static int start;

    private static int count;

    private static int version;

    private static long nextSequence = 1L;

    private static long latestWarningSequence;

    private static long latestErrorSequence;

    private static long seenSequence;

    public static int Version => Volatile.Read(in version);

    public static int Count
    {
        get
        {
            lock (gate)
            {
                return count;
            }
        }
    }

    public static RunLogLevel? Unseen
    {
        get
        {
            lock (gate)
            {
                if (latestErrorSequence > seenSequence)
                {
                    return RunLogLevel.Error;
                }
                return (latestWarningSequence > seenSequence) ? new RunLogLevel?(RunLogLevel.Warning) : ((RunLogLevel?)null);
            }
        }
    }

    public static void Verbose(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Verbose(AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Verbose, message, null, callerFile);
    }

    public static void Debug(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Debug(AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Debug, message, null, callerFile);
    }

    public static void Info(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Info(AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Info, message, null, callerFile);
    }

    public static void Warning(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Warning(AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Warning, message, null, callerFile);
    }

    public static void Warning(Exception exception, string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Warning(exception, AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Warning, message, exception.ToString(), callerFile);
    }

    public static void Error(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Error(AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Error, message, null, callerFile);
    }

    public static void Error(Exception exception, string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Log.Error(exception, AttgConstants.LogPrefix + " " + message);
        Push(RunLogLevel.Error, message, exception.ToString(), callerFile);
    }

    public static void Record(RunLogLevel level, string message, [CallerFilePath] string callerFile = "")
    {
        Push(level, message, null, callerFile);
    }

    public static void Record(RunLogLevel level, Exception exception, string message, [CallerFilePath] string callerFile = "")
    {
        Push(level, message, exception.ToString(), callerFile);
    }

    public static void Clear()
    {
        lock (gate)
        {
            start = 0;
            count = 0;
            latestWarningSequence = 0L;
            latestErrorSequence = 0L;
            version++;
        }
    }

    public static void MarkSeen()
    {
        lock (gate)
        {
            seenSequence = nextSequence - 1;
        }
    }

    public static int Snapshot(in RunLogFilter filter, RunLogLine[] destination, Span<int> levelTotals)
    {
        levelTotals.Clear();
        lock (gate)
        {
            var matchCount = 0;
            for (int i = 0; i < count; i++)
            {
                var line = lines[(start + i) % Capacity];
                levelTotals[(int)line.Level]++;
                if (filter.Matches(in line))
                {
                    destination[matchCount++] = line;
                }
            }
            return matchCount;
        }
    }

    public static string Time(DateTime atUtc)
    {
        return atUtc.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    public static string Tag(RunLogLevel level)
    {
        return level switch
        {
            RunLogLevel.Verbose => "VERB",
            RunLogLevel.Debug => "DBUG",
            RunLogLevel.Warning => "WARN",
            RunLogLevel.Error => "ERR ",
            _ => "INFO",
        };
    }

    private static void Push(RunLogLevel level, string message, string? detail, string callerFile)
    {
        lock (gate)
        {
            var utcNow = DateTime.UtcNow;
            var source = SourceName(callerFile);
            var sequence = nextSequence++;
            if (level == RunLogLevel.Warning)
            {
                latestWarningSequence = sequence;
            }
            else if (level == RunLogLevel.Error)
            {
                latestErrorSequence = sequence;
            }
            if (count > 0)
            {
                var lastIndex = (start + count - 1) % Capacity;
                var lastLine = lines[lastIndex];
                if (lastLine.Level == level && (object)lastLine.Source == source && string.Equals(lastLine.Message, message, StringComparison.Ordinal) && string.Equals(lastLine.Detail, detail, StringComparison.Ordinal))
                {
                    lines[lastIndex] = lastLine with
                    {
                        AtUtc = utcNow,
                        Repeat = lastLine.Repeat + 1
                    };
                    version++;
                    return;
                }
            }
            var writeIndex = (start + count) % Capacity;
            lines[writeIndex] = new RunLogLine(sequence, utcNow, level, source, message, detail, 1);
            if (count < Capacity)
            {
                count++;
            }
            else
            {
                start = (start + 1) % Capacity;
            }
            version++;
        }
    }

    private static string SourceName(string callerFile)
    {
        if (sourceNames.TryGetValue(callerFile, out string? sourceName))
        {
            return sourceName;
        }
        sourceName = Path.GetFileNameWithoutExtension(callerFile);
        var dotIndex = sourceName.IndexOf('.');
        if (dotIndex > 0)
        {
            sourceName = sourceName.Substring(0, dotIndex);
        }
        sourceNames[callerFile] = sourceName;
        return sourceName;
    }
}
