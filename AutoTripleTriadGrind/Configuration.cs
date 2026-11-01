using System;
using System.Collections.Generic;
using AutoTripleTriadGrind.Core;
using AutoTripleTriadGrind.Core.Changelog;
using AutoTripleTriadGrind.Core.Triad.Data;
using AutoTripleTriadGrind.Core.Triad.Decks;
using Dalamud.Configuration;
using ECommons.Throttlers;
using Newtonsoft.Json;

namespace AutoTripleTriadGrind;

public enum TriadRunMode
{
    Collect,
    Farm
}

public enum FarmStopKind
{
    Never,
    Matches
}

public enum DeckSource
{
    Optimized,
    OwnDeck
}

public enum LibraryView
{
    ByNpc,
    ByCard
}

public enum LibraryFilter
{
    All,
    Missing,
    Owned
}

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    private bool savePending;

    public int Version { get; set; }

    public bool AutoShowOnLogin { get; set; }

    public bool DragWindowFromAnywhere { get; set; }

    public string Language { get; set; } = "";

    public TriadRunMode RunMode { get; set; }

    public AfterRunAction AfterRun { get; set; }

    public string PostRunCommands { get; set; } = string.Empty;

    public bool AutoPauseInContent { get; set; } = true;

    public string LastSeenChangelogVersion { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasUnseenChangelog => !string.Equals(LastSeenChangelogVersion, ChangelogData.LatestVersion, StringComparison.Ordinal);

    public DeckSource DeckSource { get; set; }

    public int OptimizedDeckSlot { get; set; } = 4;

    public int OwnDeckSlot { get; set; }

    public int OptimizerTimeoutSeconds { get; set; } = 60;

    public int OptimizerThreads { get; set; }

    public int LossStreakSkip { get; set; } = 5;

    public int MaxMatchesPerNpc { get; set; }

    public int MaxMatchFee { get; set; }

    public int MinFreeBagSlots { get; set; } = 2;

    public int DelayBetweenMatchesMs { get; set; }

    public bool DeclinePartyInvites { get; set; }

    public int DeclineInviteDelayMinSeconds { get; set; } = 2;

    public int DeclineInviteDelayMaxSeconds { get; set; } = 6;

    public bool DeclineInviteReply { get; set; }

    public PartyInviteReplyChannel DeclineInviteReplyChannel { get; set; }

    public string DeclineInviteReplyMessage { get; set; } = "";

    public bool GmAlertStopRun { get; set; } = true;

    public bool GmAlertToast { get; set; }

    public bool GmAlertChat { get; set; }

    public bool GmAlertSound { get; set; }

    public int GmAlertBeepCount { get; set; } = 3;

    public int GmAlertBeepDurationMs { get; set; } = 250;

    public int GmAlertBeepFrequencyHz { get; set; } = 900;

    public bool GmAlertKillGame { get; set; }

    public List<string> GmAlertCommands { get; set; } = new List<string>();

    public bool AutoResumeOnFault { get; set; } = true;

    public HashSet<ushort> SelectedCards { get; set; } = new HashSet<ushort>();

    public List<uint> FarmNpcs { get; set; } = new List<uint>();

    public HashSet<uint> SkippedNpcs { get; set; } = new HashSet<uint>();

    public FarmStopKind FarmStop { get; set; } = FarmStopKind.Matches;

    public int FarmMatchLimit { get; set; } = 50;

    public Dictionary<uint, TriadRuleId[]> RegionalRules { get; set; } = new Dictionary<uint, TriadRuleId[]>();

    public Dictionary<uint, CachedDeck> OptimizedDecks { get; set; } = new Dictionary<uint, CachedDeck>();

    public LibraryView LibraryView { get; set; }

    public LibraryFilter LibraryFilter { get; set; } = LibraryFilter.Missing;

    public void MarkChangelogSeen()
    {
        if (!HasUnseenChangelog)
        {
            return;
        }
        LastSeenChangelogVersion = ChangelogData.LatestVersion;
        Save();
    }

    public void Save()
    {
        savePending = false;
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    public void SaveDebounced()
    {
        savePending = true;
        FlushPendingSave();
    }

    public void FlushPendingSave()
    {
        if (savePending && EzThrottler.Throttle(AttgConstants.ThrottleKeys.Save))
        {
            Save();
        }
    }

    public void SaveIfPending()
    {
        if (savePending)
        {
            Save();
        }
    }
}
