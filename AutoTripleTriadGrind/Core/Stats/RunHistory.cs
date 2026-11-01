using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AutoTripleTriadGrind.Core.Stats;

internal sealed class RunHistory
{
    public struct LifetimeTotals
    {
        public int Runs;

        public int Matches;

        public int Wins;

        public int Cards;

        public double Seconds;

        public readonly double WinRate
        {
            get
            {
                if (Matches <= 0)
                {
                    return 0.0;
                }
                return (double)Wins / (double)Matches;
            }
        }

        public readonly TimeSpan Duration => TimeSpan.FromSeconds(Seconds);
    }

    private const int MaxRecords = 500;

    private const string FileName = "run-history.json";

    public List<RunRecord> Records { get; private set; } = new List<RunRecord>();

    public LifetimeTotals Lifetime { get; private set; }

    private static string FilePath => Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, FileName);

    public RunHistory()
    {
        Load();
        RecomputeLifetime();
    }

    public void Append(RunRecord record)
    {
        Records.Add(record);
        Records.Sort((RunRecord left, RunRecord right) => right.EndedAtUtc.CompareTo(left.EndedAtUtc));
        if (Records.Count > MaxRecords)
        {
            Records.RemoveRange(MaxRecords, Records.Count - MaxRecords);
        }
        RecomputeLifetime();
        Save();
    }

    public void Clear()
    {
        Records.Clear();
        RecomputeLifetime();
        Save();
    }

    private void Load()
    {
        try
        {
            var filePath = FilePath;
            if (!File.Exists(filePath))
            {
                return;
            }
            var loadedRecords = JsonConvert.DeserializeObject<List<RunRecord>>(File.ReadAllText(filePath));
            if (loadedRecords != null)
            {
                Records = loadedRecords;
            }
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "RunHistory load failed; starting empty");
        }
    }

    private void RecomputeLifetime()
    {
        var lifetime = new LifetimeTotals
        {
            Runs = Records.Count
        };
        for (int i = 0; i < Records.Count; i++)
        {
            var record = Records[i];
            lifetime.Matches += record.MatchesPlayed;
            lifetime.Wins += record.MatchesWon;
            lifetime.Cards += record.CardsObtained.Count;
            lifetime.Seconds += record.DurationSeconds;
        }
        Lifetime = lifetime;
    }

    private void Save()
    {
        try
        {
            var configDirectory = Plugin.PluginInterface.ConfigDirectory;
            if (!configDirectory.Exists)
            {
                configDirectory.Create();
            }
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Records, Formatting.Indented));
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "RunHistory save failed");
        }
    }
}
