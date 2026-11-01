using System;

namespace AutoTripleTriadGrind.Core;

internal static class AttgConstants
{
    internal static class ThrottleKeys
    {
        public const string Save = "AutoTripleTriadGrind.Save";
    }

    public const string PrimaryCommand = "/tta";

    public const string AliasCommand = "/triadautopilot";

    public const string LogPrefix = "[TTA]";

    public const int SaveThrottleMs = 500;

    public static readonly int BackgroundWorkerThreads = Math.Max(1, Environment.ProcessorCount / 4);
}

public enum RunLogLevel : byte
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}

public readonly record struct RunLogLine(long Sequence, DateTime AtUtc, RunLogLevel Level, string Source, string Message, string? Detail, int Repeat);
