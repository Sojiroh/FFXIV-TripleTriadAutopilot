using System;
using AutoTripleTriadGrind.Core.Localization;

namespace AutoTripleTriadGrind.Core.Changelog;

internal static class ChangelogData
{
    public static readonly ChangelogEntry[] Entries = new ChangelogEntry[8]
    {
        new ChangelogEntry("1.3.1.0", "2026-10-22", L.Changelog.Release1310),
        new ChangelogEntry("1.3.0.0", "2026-10-21", L.Changelog.Release1300),
        new ChangelogEntry("1.2.2.0", "2026-10-21", L.Changelog.Release1220),
        new ChangelogEntry("1.2.1.0", "2026-10-18", L.Changelog.Release1210),
        new ChangelogEntry("1.2.0.0", "2026-10-13", L.Changelog.Release1200),
        new ChangelogEntry("1.1.1.0", "2026-09-29", L.Changelog.Release1110),
        new ChangelogEntry("1.1.0.0", "2026-09-24", L.Changelog.Release1100),
        new ChangelogEntry("1.0.0.0", "2026-09-24", L.Changelog.Release1000)
    };

    public static string LatestVersion => Entries[0].Version;

    public static int UnseenCount(string lastSeenVersion)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (string.Equals(Entries[i].Version, lastSeenVersion, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return Entries.Length;
    }
}
