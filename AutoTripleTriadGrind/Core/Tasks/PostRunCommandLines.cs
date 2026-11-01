using System;

namespace AutoTripleTriadGrind.Core.Tasks;

internal static class PostRunCommandLines
{
    private const char CommandPrefix = '/';

    public static string[] Split(string commandBlock)
    {
        if (!string.IsNullOrWhiteSpace(commandBlock))
        {
            return commandBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        return Array.Empty<string>();
    }

    public static bool IsCommand(string line)
    {
        if (line.Length > 0)
        {
            return line[0] == '/';
        }
        return false;
    }
}
