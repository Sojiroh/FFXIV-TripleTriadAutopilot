namespace AutoTripleTriadGrind.Core.Localization;

internal static class L
{
    internal static class Collection
    {
        public static readonly LocString Title = new LocString("collection.title", "NPC cards");

        public static readonly LocString Loading = new LocString("collection.loading", "Reading the Triple Triad tables…");

        public static readonly LocString ViewByNpc = new LocString("collection.view.byNpc", "By NPC");

        public static readonly LocString ViewByCard = new LocString("collection.view.byCard", "By card");

        public static readonly LocString FilterMissing = new LocString("collection.filter.missing", "Missing");

        public static readonly LocString FilterAll = new LocString("collection.filter.all", "All");

        public static readonly LocString FilterOwned = new LocString("collection.filter.owned", "Owned");

        public static readonly LocString SearchHint = new LocString("collection.searchHint", "Search NPCs, zones, or cards");

        public static readonly LocString Summary = new LocString("collection.summary", "{0} picked  ·  {1} of {2} NPC cards owned");

        public static readonly LocString SelectAllMissing = new LocString("collection.selectAllMissing", "Pick every missing card");

        public static readonly LocString NothingHere = new LocString("collection.nothingHere", "Nothing to show with this filter.");

        public static readonly LocString TooltipFee = new LocString("collection.tooltip.fee", "Match fee: {0} MGP");

        public static readonly LocString TooltipRules = new LocString("collection.tooltip.rules", "Rules: {0}");

        public static readonly LocString TooltipRegional = new LocString("collection.tooltip.regional", "Also plays the regional rules of its area.");

        public static readonly LocString TooltipLockedQuest = new LocString("collection.tooltip.lockedQuest", "Locked: complete “{0}” to unlock this NPC.");

        public static readonly LocString TooltipLockedAnyQuest = new LocString("collection.tooltip.lockedAnyQuest", "Locked: complete any one of these quests to unlock this NPC: {0}");

        public static readonly LocString TooltipSelectNpc = new LocString("collection.tooltip.selectNpc", "Click to pick or drop every card you are missing from this NPC.");

        public static readonly LocString TooltipSkipNpc = new LocString("collection.tooltip.skipNpc", "Right-click to skip this NPC in every run.");

        public static readonly LocString TooltipUnskipNpc = new LocString("collection.tooltip.unskipNpc", "Right-click to play this NPC again.");

        public static readonly LocString TooltipOwned = new LocString("collection.tooltip.owned", "Owned");

        public static readonly LocString TooltipSelected = new LocString("collection.tooltip.selected", "Picked. Click to drop it from the plan.");

        public static readonly LocString TooltipMissing = new LocString("collection.tooltip.missing", "Missing. Click to pick it.");

        public static readonly LocString TooltipDroppedBy = new LocString("collection.tooltip.droppedBy", "{0}  ·  {1}");
    }

    internal static class Farm
    {
        public static readonly LocString Title = new LocString("farm.title", "NPCs to farm");

        public static readonly LocString StopLabel = new LocString("farm.stopLabel", "Play each NPC");

        public static readonly LocString StopAfterMatches = new LocString("farm.stop.matches", "A set number of times");

        public static readonly LocString StopNever = new LocString("farm.stop.never", "Until I stop");

        public static readonly LocString MatchesFormat = new LocString("farm.matchesFormat", "{0} matches");
    }

    internal static class Common
    {
        public static readonly LocString Close = new LocString("common.close", "Close");

        public static readonly LocString Cancel = new LocString("common.cancel", "Cancel");

        public static readonly LocString Clear = new LocString("common.clear", "Clear");

        public static readonly LocString SelectAll = new LocString("common.selectAll", "Select all");

        public static readonly LocString Pause = new LocString("common.pause", "Pause");

        public static readonly LocString Resume = new LocString("common.resume", "Resume");

        public static readonly LocString StopRun = new LocString("common.stopRun", "Stop the run");

        public static readonly LocString Working = new LocString("common.working", "Working…");

        public static readonly LocString PlayerNotLoaded = new LocString("common.playerNotLoaded", "Player not loaded.");

        public static readonly LocString DragAdjustHint = new LocString("common.dragAdjustHint", "Drag to adjust · Ctrl+click to type");

        public static readonly LocString NoMatches = new LocString("common.noMatches", "Nothing matches “{0}”.");
    }

    internal static class Shell
    {
        public static readonly LocString NavTriad = new LocString("shell.nav.triad", "Triple Triad");

        public static readonly LocString NavSettings = new LocString("shell.nav.settings", "Settings");

        public static readonly LocString NavHistory = new LocString("shell.nav.history", "History");

        public static readonly LocString NavPlugins = new LocString("shell.nav.plugins", "Plugins");

        public static readonly LocString NavLog = new LocString("shell.nav.log", "Console");

        public static readonly LocString NavChangelog = new LocString("shell.nav.changelog", "Changelog");

        public static readonly LocString NavAbout = new LocString("shell.nav.about", "About");

        public static readonly LocString StatusRunning = new LocString("shell.status.running", "Running");

        public static readonly LocString StatusPaused = new LocString("shell.status.paused", "Paused");

        public static readonly LocString StatusReady = new LocString("shell.status.ready", "Ready");

        public static readonly LocString StatusPickCards = new LocString("shell.status.pickCards", "Pick cards");

        public static readonly LocString StatusPickNpcs = new LocString("shell.status.pickNpcs", "Pick NPCs");

        public static readonly LocString StatusBlocked = new LocString("shell.status.blocked", "Nothing reachable");

        public static readonly LocString StatusAllDone = new LocString("shell.status.allDone", "All done");

        public static readonly LocString StatusSetupNeeded = new LocString("shell.status.setupNeeded", "Setup needed");

        public static readonly LocString StatusIdle = new LocString("shell.status.idle", "Idle");

        public static readonly LocString Minimize = new LocString("shell.minimize", "Minimize to the title strip");

        public static readonly LocString Restore = new LocString("shell.restore", "Restore the window");

        public static readonly LocString ResumeBlocked = new LocString("shell.resumeBlocked", "Resumes automatically once you leave the duty");

        public static readonly LocString GreetingMorning = new LocString("shell.greeting.morning", "Good morning");

        public static readonly LocString GreetingAfternoon = new LocString("shell.greeting.afternoon", "Good afternoon");

        public static readonly LocString GreetingEvening = new LocString("shell.greeting.evening", "Good evening");

        public static readonly LocString GreetingNight = new LocString("shell.greeting.night", "Late night");
    }

    internal static class Triad
    {
        public static readonly LocString TitleSetupNeeded = new LocString("triad.title.setupNeeded", "Setup needed");

        public static readonly LocString DetailSetupNeeded = new LocString("triad.detail.setupNeeded", "Install the required plugins before your first run.");

        public static readonly LocString TitleDataUnavailable = new LocString("triad.title.dataUnavailable", "Game data unavailable");

        public static readonly LocString DetailDataUnavailable = new LocString("triad.detail.dataUnavailable", "The Triple Triad tables could not be read. The plugin log has the details.");

        public static readonly LocString TitlePickCards = new LocString("triad.title.pickCards", "Pick the cards you want");

        public static readonly LocString DetailPickCards = new LocString("triad.detail.pickCards", "Tick cards or whole NPCs below and they'll appear in your plan.");

        public static readonly LocString TitlePickNpcs = new LocString("triad.title.pickNpcs", "Pick NPCs to farm");

        public static readonly LocString DetailPickNpcs = new LocString("triad.detail.pickNpcs", "Tick the NPCs below you want to play over and over.");

        public static readonly LocString TitleNothingReachable = new LocString("triad.title.nothingReachable", "Nothing to play yet");

        public static readonly LocString DetailNothingReachable = new LocString("triad.detail.nothingReachable", "Every NPC for your picks is locked or excluded. Unlock them first.");

        public static readonly LocString TitleAllDone = new LocString("triad.title.allDone", "All collected");

        public static readonly LocString DetailAllDone = new LocString("triad.detail.allDone", "You already own every card you picked.");

        public static readonly LocString TitleReady = new LocString("triad.title.ready", "Ready to play");

        public static readonly LocString DetailReady = new LocString("triad.detail.ready", "Everything's set. Press Start whenever you're ready.");

        public static readonly LocString TitleRunning = new LocString("triad.title.running", "Playing");

        public static readonly LocString TitlePaused = new LocString("triad.title.paused", "Paused");

        public static readonly LocString DetailPausedInContent = new LocString("triad.detail.pausedInContent", "Resumes once you leave the duty");

        public static readonly LocString DetailPausedManual = new LocString("triad.detail.pausedManual", "Resume whenever you're ready");

        public static readonly LocString OpenPlugins = new LocString("triad.openPlugins", "Open plugins");

        public static readonly LocString NoRunsYet = new LocString("triad.noRunsYet", "No runs yet");

        public static readonly LocString StatsAppearHere = new LocString("triad.statsAppearHere", "your stats will appear here");

        public static readonly LocString LastRun = new LocString("triad.lastRun", "Last run  ·  {0} cards");

        public static readonly LocString LastRunDetail = new LocString("triad.lastRunDetail", "{0}  ·  {1} of {2} matches won");

        public static readonly LocString ModeCollect = new LocString("triad.mode.collect", "Collect");

        public static readonly LocString ModeFarm = new LocString("triad.mode.farm", "Farm");

        public static readonly LocString Plan = new LocString("triad.plan", "Plan");

        public static readonly LocString SentenceCollect = new LocString("triad.sentence.collect", "Collect");

        public static readonly LocString SentenceFarm = new LocString("triad.sentence.farm", "Farm");

        public static readonly LocString SentenceThen = new LocString("triad.sentence.then", "then");

        public static readonly LocString SentenceEnd = new LocString("triad.sentence.end", ".");

        public static readonly LocString CardsNone = new LocString("triad.cardsNone", "no cards yet");

        public static readonly LocPlural CardsCount = new LocPlural("triad.cardsCount", "{0} card", "{0} cards");

        public static readonly LocString NpcsNone = new LocString("triad.npcsNone", "no NPCs yet");

        public static readonly LocPlural NpcsCount = new LocPlural("triad.npcsCount", "{0} NPC", "{0} NPCs");

        public static readonly LocString StartSub = new LocString("triad.startSub", "{0}  ·  {1}");

        public static readonly LocString QueueCaption = new LocString("triad.queueCaption", "Route: {0}");

        public static readonly LocPlural QueueMore = new LocPlural("triad.queueMore", "and {0} more", "and {0} more");

        public static readonly LocPlural UnavailableCaption = new LocPlural("triad.unavailableCaption", "{0} picked card has no NPC you can play right now.", "{0} picked cards have no NPC you can play right now.");

        public static readonly LocPlural FarmCaptionMatches = new LocPlural("triad.farmCaption.matches", "Each NPC is played {0} time.", "Each NPC is played {0} times.");

        public static readonly LocString FarmCaptionNever = new LocString("triad.farmCaption.never", "Plays until you stop the run.");

        public static readonly LocString PlanLocked = new LocString("triad.planLocked", "Stop the run to change the plan.");

        public static readonly LocString AfterStayToken = new LocString("triad.after.stay.token", "stay where you are");

        public static readonly LocString AfterStayName = new LocString("triad.after.stay.name", "Stay where you are");

        public static readonly LocString AfterStayDetail = new LocString("triad.after.stay.detail", "Just stop. You're left standing by the last NPC.");

        public static readonly LocString AfterInnToken = new LocString("triad.after.inn.token", "return to the inn");

        public static readonly LocString AfterInnName = new LocString("triad.after.inn.name", "Return to the inn");

        public static readonly LocString AfterInnDetail = new LocString("triad.after.inn.detail", "Travel to your Grand Company city and enter the inn room.");

        public static readonly LocString AfterLogoutToken = new LocString("triad.after.logout.token", "log out");

        public static readonly LocString AfterLogoutName = new LocString("triad.after.logout.name", "Log out to title");

        public static readonly LocString AfterLogoutDetail = new LocString("triad.after.logout.detail", "Log out to the title screen.");

        public static readonly LocString AfterCloseToken = new LocString("triad.after.close.token", "close the game");

        public static readonly LocString AfterCloseName = new LocString("triad.after.close.name", "Close the game");

        public static readonly LocString AfterCloseDetail = new LocString("triad.after.close.detail", "Close FFXIV entirely (via XIVLauncher's /xlkill).");

        public static readonly LocString WhenDone = new LocString("triad.whenDone", "When the run is done");

        public static readonly LocString ReasonInstall = new LocString("triad.reason.install", "install the required plugins");

        public static readonly LocString ReasonData = new LocString("triad.reason.data", "the game data could not be read");

        public static readonly LocString ReasonPickCard = new LocString("triad.reason.pickCard", "pick at least one card");

        public static readonly LocString ReasonPickNpc = new LocString("triad.reason.pickNpc", "pick at least one NPC");

        public static readonly LocString ReasonUnreachable = new LocString("triad.reason.unreachable", "no NPC for your picks can be played yet");

        public static readonly LocString ReasonAllOwned = new LocString("triad.reason.allOwned", "you own every card you picked");

        public static readonly LocString StateRunning = new LocString("triad.state.running", "running");

        public static readonly LocString StatePaused = new LocString("triad.state.paused", "paused");

        public static readonly LocString StopSub = new LocString("triad.stopSub", "{0} · {1}");

        public static readonly LocString Start = new LocString("triad.start", "START");

        public static readonly LocString Stop = new LocString("triad.stop", "STOP");

        public static readonly LocString PauseCaps = new LocString("triad.pause", "PAUSE");

        public static readonly LocString ResumeCaps = new LocString("triad.resume", "RESUME");

        public static readonly LocString InContent = new LocString("triad.inContent", "in content");

        public static readonly LocString ExpansionArr = new LocString("triad.expansion.arr", "A Realm Reborn");

        public static readonly LocString ExpansionHw = new LocString("triad.expansion.hw", "Heavensward");

        public static readonly LocString ExpansionSb = new LocString("triad.expansion.sb", "Stormblood");

        public static readonly LocString ExpansionShb = new LocString("triad.expansion.shb", "Shadowbringers");

        public static readonly LocString ExpansionEw = new LocString("triad.expansion.ew", "Endwalker");

        public static readonly LocString ExpansionDt = new LocString("triad.expansion.dt", "Dawntrail");

        public static readonly LocString NpcLocation = new LocString("triad.npcLocation", "{0}  ({1}, {2})");

        public static readonly LocString CardStats = new LocString("triad.cardStats", "{0}-star  ·  {1} {2} {3} {4}");

        public static readonly LocString SkipLocked = new LocString("triad.skip.locked", "Locked");

        public static readonly LocString SkipBattleHall = new LocString("triad.skip.battleHall", "Battle Hall");

        public static readonly LocString SkipLeaveBattleHall = new LocString("triad.skip.leaveBattleHall", "Leave the Battle Hall");

        public static readonly LocString SkipFee = new LocString("triad.skip.fee", "Fee too high");

        public static readonly LocString SkipMgp = new LocString("triad.skip.mgp", "Not enough MGP");

        public static readonly LocString SkipInteract = new LocString("triad.skip.interact", "Could not challenge");

        public static readonly LocString SkipLossStreak = new LocString("triad.skip.lossStreak", "Too many losses");

        public static readonly LocString SkipMatchLimit = new LocString("triad.skip.matchLimit", "Match limit reached");

        public static readonly LocString SkipInventory = new LocString("triad.skip.inventory", "Inventory full");

        public static readonly LocString SkipUnreachable = new LocString("triad.skip.unreachable", "Unreachable");

        public static readonly LocString SkipByYou = new LocString("triad.skip.byYou", "Skipped");

        public static readonly LocString SkipQuestOffered = new LocString("triad.skip.questOffered", "Quest in the way");

        public static readonly LocString SkipNpcAlways = new LocString("triad.npcMenu.skip", "Skip this NPC in every run");

        public static readonly LocString PlayNpcAgain = new LocString("triad.npcMenu.playAgain", "Play this NPC again");
    }

    internal static class Run
    {
        public static readonly LocString PhaseRegistering = new LocString("run.phase.registering", "Registering cards");

        public static readonly LocString PhasePlanning = new LocString("run.phase.planning", "Planning the route");

        public static readonly LocString PhaseOptimizing = new LocString("run.phase.optimizing", "Building a deck");

        public static readonly LocString PhaseTravelling = new LocString("run.phase.travelling", "Travelling");

        public static readonly LocString PhaseChallenging = new LocString("run.phase.challenging", "Challenging");

        public static readonly LocString PhasePlaying = new LocString("run.phase.playing", "Playing");

        public static readonly LocString PhaseRematch = new LocString("run.phase.rematch", "Rematch");

        public static readonly LocString PhaseFinishing = new LocString("run.phase.finishing", "Finishing up");

        public static readonly LocString PhaseStandingBy = new LocString("run.phase.standingBy", "Standing by");

        public static readonly LocString PhaseReady = new LocString("run.phase.ready", "Ready");

        public static readonly LocString PhasePaused = new LocString("run.phase.paused", "Paused");

        public static readonly LocString PhasePausedInContent = new LocString("run.phase.pausedInContent", "Paused (in content)");

        public static readonly LocString Footer = new LocString("run.footer", "NPC {0} of {1}");

        public static readonly LocString Record = new LocString("run.record", "{0}W  {1}L  {2}D");

        public static readonly LocString UpNext = new LocString("run.upNext", "Up next");

        public static readonly LocString QueuePlanning = new LocString("run.queuePlanning", "The route shows up here once it is planned.");

        public static readonly LocString QueueDone = new LocString("run.queueDone", "Every NPC on the route is done.");

        public static readonly LocString Skipped = new LocString("run.skipped", "Skipped");

        public static readonly LocString TileMatches = new LocString("run.tile.matches", "Matches");

        public static readonly LocString TileCards = new LocString("run.tile.cards", "Cards");

        public static readonly LocString TileNpcs = new LocString("run.tile.npcs", "NPCs done");

        public static readonly LocString TileElapsed = new LocString("run.tile.elapsed", "Elapsed");

        public static readonly LocString GoalOf = new LocString("run.goal.of", "/ {0}");
    }

    internal static class History
    {
        public static readonly LocString Title = new LocString("history.title", "History");

        public static readonly LocString Empty = new LocString("history.empty", "Your finished runs will show up here.");

        public static readonly LocPlural Summary = new LocPlural("history.summary", "{0} recorded run  ·  {1} playing  ·  {2}% of matches won", "{0} recorded runs  ·  {1} playing  ·  {2}% of matches won");

        public static readonly LocString TileRuns = new LocString("history.tile.runs", "Runs");

        public static readonly LocString TileMatches = new LocString("history.tile.matches", "Matches");

        public static readonly LocString TileWins = new LocString("history.tile.wins", "Wins");

        public static readonly LocString TileCards = new LocString("history.tile.cards", "Cards");

        public static readonly LocString TileNpcs = new LocString("history.tile.npcs", "NPCs");

        public static readonly LocString TileTime = new LocString("history.tile.time", "Time");

        public static readonly LocString NoRuns = new LocString("history.noRuns", "No runs recorded yet. Finish (or stop) a run and it'll show up here.");

        public static readonly LocString CardsPerRun = new LocString("history.cardsPerRun", "Cards per run");

        public static readonly LocString RecentRuns = new LocString("history.recentRuns", "Recent runs");

        public static readonly LocPlural ChartRange = new LocPlural("history.chartRange", "last {0} run  ·  oldest to newest", "last {0} runs  ·  oldest to newest");

        public static readonly LocString ChartPeak = new LocString("history.chartPeak", "peak {0}");

        public static readonly LocString ChartTooltip = new LocString("history.chartTooltip", "{0}  ·  {1} cards  ·  {2} matches  ·  {3}");

        public static readonly LocString RowDetail = new LocString("history.rowDetail", "{0}  ·  {1}");

        public static readonly LocString TooltipRecord = new LocString("history.tooltip.record", "Matches: {0} won, {1} lost, {2} drawn");

        public static readonly LocPlural TooltipSkipped = new LocPlural("history.tooltip.skipped", "{0} NPC skipped", "{0} NPCs skipped");

        public static readonly LocString TooltipCards = new LocString("history.tooltip.cards", "Cards: {0}");

        public static readonly LocString JustNow = new LocString("history.time.justNow", "just now");

        public static readonly LocString MinutesAgo = new LocString("history.time.minutesAgo", "{0}m ago");

        public static readonly LocString HoursAgo = new LocString("history.time.hoursAgo", "{0}h ago");

        public static readonly LocString DaysAgo = new LocString("history.time.daysAgo", "{0}d ago");

        public static readonly LocString ClearHistory = new LocString("history.clear", "Clear history");

        public static readonly LocString ClearQuestion = new LocString("history.clearQuestion", "Delete all recorded runs?");

        public static readonly LocString ClearYes = new LocString("history.clearYes", "Yes, clear");
    }

    internal static class Plugins
    {
        public static readonly LocString Title = new LocString("plugins.title", "Plugins");

        public static readonly LocString AllInstalled = new LocString("plugins.allInstalled", "All required plugins are installed and loaded.");

        public static readonly LocPlural Missing = new LocPlural("plugins.missing", "{0} required plugin is missing.", "{0} required plugins are missing.");

        public static readonly LocString Required = new LocString("plugins.required", "Required");

        public static readonly LocString Optional = new LocString("plugins.optional", "Optional");

        public static readonly LocString Installed = new LocString("plugins.installed", "Installed");

        public static readonly LocString Install = new LocString("plugins.install", "Install");

        public static readonly LocString Installing = new LocString("plugins.installing", "Installing…");

        public static readonly LocString RepoHint = new LocString("plugins.repoHint", "Repo: {0}\nLeft-click to open repo URL · right-click to copy");

        public static readonly LocString Footer = new LocString("plugins.footer", "Install adds the plugin's source repository to Dalamud and queues an install. If one-click install fails (URL drift, network), right-click a plugin name to copy its repo URL and add it manually via /xlsettings -> Experimental -> Custom Plugin Repositories.");

        public static readonly LocString PurposeVnavmesh = new LocString("plugins.purpose.vnavmesh", "Pathfinding, flying, and movement to Triple Triad NPCs.");
    }

    internal static class Log
    {
        public static readonly LocString Title = new LocString("log.title", "Console");

        public static readonly LocPlural Entries = new LocPlural("log.entries", "{0} line in the buffer", "{0} lines in the buffer");

        public static readonly LocString Empty = new LocString("log.empty", "Nothing logged yet. Start a run and every step the plugin takes shows up here.");

        public static readonly LocString NoMatches = new LocString("log.noMatches", "No lines match the current filters.");

        public static readonly LocString Footer = new LocString("log.footer", "Every line also goes to the Dalamud log (/xllog) with the {0} prefix. When reporting a bug, press Copy log and paste the result into the issue.");

        public static readonly LocString SearchHint = new LocString("log.searchHint", "Search messages and sources");

        public static readonly LocString CopyAll = new LocString("log.copyAll", "Copy log");

        public static readonly LocPlural CopyFiltered = new LocPlural("log.copyFiltered", "Copy {0} line", "Copy {0} lines");

        public static readonly LocString CopyTooltip = new LocString("log.copyTooltip", "Copies the lines shown below with a header naming the plugin version, Dalamud version and zone, ready to paste into a bug report.");

        public static readonly LocString Copied = new LocString("log.copied", "Copied");

        public static readonly LocPlural CopiedLines = new LocPlural("log.copiedLines", "Copied {0} line to the clipboard", "Copied {0} lines to the clipboard");

        public static readonly LocString Clear = new LocString("log.clear", "Clear");

        public static readonly LocString ConfirmClear = new LocString("log.confirmClear", "Click again to clear");

        public static readonly LocString Close = new LocString("log.close", "Close");

        public static readonly LocString LevelVerbose = new LocString("log.level.verbose", "Verbose");

        public static readonly LocString LevelDebug = new LocString("log.level.debug", "Debug");

        public static readonly LocString LevelInfo = new LocString("log.level.info", "Info");

        public static readonly LocString LevelWarning = new LocString("log.level.warning", "Warnings");

        public static readonly LocString LevelError = new LocString("log.level.error", "Errors");

        public static readonly LocString LevelTooltip = new LocString("log.levelTooltip", "Click to show or hide these lines. Shift-click to show only this level.");

        public static readonly LocString SourceChip = new LocString("log.sourceChip", "Source: {0}");

        public static readonly LocString SourceChipTooltip = new LocString("log.sourceChipTooltip", "Click to stop filtering by source.");

        public static readonly LocString JumpLatest = new LocString("log.jumpLatest", "Jump to latest");

        public static readonly LocPlural NewLines = new LocPlural("log.newLines", "{0} new line", "{0} new lines");

        public static readonly LocString Showing = new LocString("log.showing", "Showing {0} of {1}");

        public static readonly LocString ResetFilters = new LocString("log.resetFilters", "Reset filters");

        public static readonly LocString Shortcuts = new LocString("log.shortcuts", "Ctrl+F search · Ctrl+C copy · Shift-click selects a range · Double-click copies a line");

        public static readonly LocPlural Selected = new LocPlural("log.selected", "{0} line selected", "{0} lines selected");

        public static readonly LocString CopySelection = new LocString("log.copySelection", "Copy selection");

        public static readonly LocString ClearSelection = new LocString("log.clearSelection", "Clear selection");

        public static readonly LocString CopyLine = new LocString("log.copyLine", "Copy line");

        public static readonly LocString CopyToEnd = new LocString("log.copyToEnd", "Copy from here to the end");

        public static readonly LocString OnlySource = new LocString("log.onlySource", "Show only {0}");

        public static readonly LocString Repeated = new LocString("log.repeated", "Repeated {0} times in a row");

        public static readonly LocString HasDetails = new LocString("log.hasDetails", "Has a stack trace. Select the line to read it.");
    }

    internal static class Changelog
    {
        public static readonly LocString Title = new LocString("changelog.title", "What's new");

        public static readonly LocString Subtitle = new LocString("changelog.subtitle", "Every update, newest first.");

        public static readonly LocString Version = new LocString("changelog.version", "Version {0}");

        public static readonly LocString Latest = new LocString("changelog.latest", "Latest");

        public static readonly LocString New = new LocString("changelog.new", "New");

        public static readonly LocPlural Changes = new LocPlural("changelog.changes", "{0} change", "{0} changes");

        public static readonly LocString[] Release1310 = new LocString[1]
        {
            new LocString("changelog.r1310.1", "Fixed the walk up to an NPC behind a counter, such as Momodi, running into the counter on every try: it now stops in front of the counter once the NPC is in talking range")
        };

        public static readonly LocString[] Release1300 = new LocString[5]
        {
            new LocString("changelog.r1300.1", "Added Chat commands to the settings: list your own commands, one per line (such as /li home or /ays m), and they run when a run finishes on its own, before the after-run action, never after Stop or a failure"),
            new LocString("changelog.r1300.2", "Fixed the deck being built again at the table after it was already built on the way to the NPC: each NPC now gets one deck build"),
            new LocString("changelog.r1300.3", "Fixed the run hanging when an NPC offers a quest instead of the challenge: the offer is declined and the NPC is skipped as Quest in the way"),
            new LocString("changelog.r1300.4", "Fixed a run waiting at a hub aetheryte forever when none of the aethernet stops into a zone are attuned, as with the Idyllshire gates to the Hinterlands: it now stops with a message saying a stop needs attuning"),
            new LocString("changelog.r1300.5", "The main window now moves only when you drag its top bar, so a drag inside it no longer moves it by accident; turn on Drag from anywhere in the settings for the old behaviour")
        };

        public static readonly LocString[] Release1220 = new LocString[2]
        {
            new LocString("changelog.r1220.1", "Fixed long sessions ending in a crash or a Start button that stops working; moving around no longer sets up new game hooks each time"),
            new LocString("changelog.r1220.2", "Fixed the quest level sync icon not showing before level-synced quest names in NPC unlock hints. Contributed by lizvik")
        };

        public static readonly LocString[] Release1210 = new LocString[1]
        {
            new LocString("changelog.r1210.1", "Added an entry route into Terncliff, so the NPCs there can be played. Contributed by lizvik")
        };

        public static readonly LocString[] Release1200 = new LocString[13]
        {
            new LocString("changelog.r1200.1", "Added entry routes into Fortemps Manor, Gangos and Lower Jeuno, so the NPCs there can be played. Contributed by lizvik"),
            new LocString("changelog.r1200.2", "The Battle Hall NPCs can now be played while you are inside the Battle Hall"),
            new LocString("changelog.r1200.3", "Added a right-click option to skip an NPC in every run"),
            new LocString("changelog.r1200.4", "Fixed an issue where the run kept challenging an NPC after winning its last card, even when the card could not be registered. Contributed by lizvik"),
            new LocString("changelog.r1200.5", "Fixed an issue where an NPC's menu stayed open after a match and blocked teleporting to the next NPC"),
            new LocString("changelog.r1200.6", "NPCs that turn the challenge down are now skipped within seconds instead of being retried for a minute and a half"),
            new LocString("changelog.r1200.7", "Fixed NPCs that accept any one of several unlock quests showing as locked, and some locked NPCs showing as unlocked"),
            new LocString("changelog.r1200.8", "Fixed an issue where the Triple Triad challenge was never chosen on the Japanese client"),
            new LocString("changelog.r1200.9", "Fixed missing characters in NPC and zone names on Chinese clients. Contributed by lizvik"),
            new LocString("changelog.r1200.10", "Fixed some NPCs being looked for on the wrong map. Contributed by lizvik"),
            new LocString("changelog.r1200.11", "Fixed NPCs whose path ends a few metres short being reported as unreachable. Contributed by lizvik"),
            new LocString("changelog.r1200.12", "The optimized deck is now built once per NPC instead of again after every card won from it"),
            new LocString("changelog.r1200.13", "The deck optimizer now uses a quarter of your CPU threads by default, keeping the game smooth")
        };

        public static readonly LocString[] Release1110 = new LocString[1]
        {
            new LocString("changelog.r1110.1", "Added a Buy Me a Coffee button under Patreon on the About page")
        };

        public static readonly LocString[] Release1100 = new LocString[1]
        {
            new LocString("changelog.r1100.1", "Fixed an issue where the character could stop too far from an NPC to talk to it and then skip that NPC")
        };

        public static readonly LocString[] Release1000 = new LocString[1]
        {
            new LocString("changelog.r1000.1", "Initial release")
        };
    }

    internal static class About
    {
        public static readonly LocString LinkHint = new LocString("about.linkHint", "Click to open · right-click to copy");

        public static readonly LocString MadeBy = new LocString("about.madeBy", "Made by {0}");

        public static readonly LocString Version = new LocString("about.version", "v {0}");

        public static readonly LocString Community = new LocString("about.community", "Community");

        public static readonly LocString GitHubTitle = new LocString("about.githubTitle", "View on GitHub");

        public static readonly LocString GitHubBody = new LocString("about.githubBody", "Browse the source code and every release.");

        public static readonly LocString ReminderTitle = new LocString("about.reminder.title", "A little reminder");

        public static readonly LocString FactsTitle = new LocString("about.facts.title", "Did you know?");

        public static readonly LocString QuotesTitle = new LocString("about.quotes.title", "Words to live by");

        public static readonly LocString JokesTitle = new LocString("about.jokes.title", "Just for fun");

        public static readonly LocString[] Reminders = new LocString[12]
        {
            new LocString("about.reminder.1", "Been at it a while? Roll your shoulders and take one slow breath."),
            new LocString("about.reminder.2", "Hydration check. When did you last drink some water?"),
            new LocString("about.reminder.3", "Blink a few times and let your eyes rest for a moment."),
            new LocString("about.reminder.4", "Stand up, stretch, and shake out your hands. Future you says thanks."),
            new LocString("about.reminder.5", "Sit up and settle in comfortably. Your back will thank you later."),
            new LocString("about.reminder.6", "Remember to eat something today. You matter more than any score."),
            new LocString("about.reminder.7", "Eyes feel tired? Look at something far away for twenty seconds."),
            new LocString("about.reminder.8", "Whatever you're chasing, you're allowed to take a break whenever."),
            new LocString("about.reminder.9", "You're doing great. Be a little kinder to yourself today."),
            new LocString("about.reminder.10", "A glass of water and a quick stretch can reset a long session."),
            new LocString("about.reminder.11", "Unclench your jaw and drop your shoulders. There you go."),
            new LocString("about.reminder.12", "Rest is part of the journey too. Step away whenever you need to.")
        };

        public static readonly LocString[] Facts = new LocString[12]
        {
            new LocString("about.facts.1", "Honey never spoils. Jars over 3,000 years old have been found still edible."),
            new LocString("about.facts.2", "Octopuses have three hearts and blue blood."),
            new LocString("about.facts.3", "A day on Venus is longer than a whole year on Venus."),
            new LocString("about.facts.4", "Bananas are berries, but strawberries aren't."),
            new LocString("about.facts.5", "There are more possible chess games than atoms in the observable universe."),
            new LocString("about.facts.6", "Sharks have been around longer than trees have."),
            new LocString("about.facts.7", "A group of flamingos is called a flamboyance."),
            new LocString("about.facts.8", "Honeybees can recognize individual human faces."),
            new LocString("about.facts.9", "Wombat droppings are cube shaped."),
            new LocString("about.facts.10", "The Eiffel Tower can grow over 15 cm taller on a hot day."),
            new LocString("about.facts.11", "Hot water can sometimes freeze faster than cold water."),
            new LocString("about.facts.12", "A bolt of lightning is roughly five times hotter than the surface of the Sun.")
        };

        public static readonly LocString[] Quotes = new LocString[10]
        {
            new LocString("about.quotes.1", "Done is better than perfect. You can always polish later."),
            new LocString("about.quotes.2", "Small steps every day add up to surprising distances."),
            new LocString("about.quotes.3", "Comparison is the thief of joy. Run your own race."),
            new LocString("about.quotes.4", "Progress, not perfection."),
            new LocString("about.quotes.5", "You don't have to be great to start, but you have to start to be great."),
            new LocString("about.quotes.6", "Be patient with yourself. Growth takes time."),
            new LocString("about.quotes.7", "The best time to begin was yesterday. The second best is right now."),
            new LocString("about.quotes.8", "Celebrate the small wins. They count too."),
            new LocString("about.quotes.9", "Slow progress is still progress."),
            new LocString("about.quotes.10", "Your only real competition is who you were yesterday.")
        };

        public static readonly LocString[] Jokes = new LocString[10]
        {
            new LocString("about.jokes.1", "Why don't scientists trust atoms? Because they make up everything."),
            new LocString("about.jokes.2", "I would tell you a chemistry joke, but I know I wouldn't get a reaction."),
            new LocString("about.jokes.3", "Why did the scarecrow win an award? He was outstanding in his field."),
            new LocString("about.jokes.4", "I'm reading a book about anti-gravity. It's impossible to put down."),
            new LocString("about.jokes.5", "Why don't skeletons fight each other? They don't have the guts."),
            new LocString("about.jokes.6", "What do you call fake spaghetti? An impasta."),
            new LocString("about.jokes.7", "Why did the bicycle fall over? It was two tired."),
            new LocString("about.jokes.8", "What do you call cheese that isn't yours? Nacho cheese."),
            new LocString("about.jokes.9", "I'm on a seafood diet. I see food, and I eat it."),
            new LocString("about.jokes.10", "I only know 25 letters of the alphabet. I don't know y.")
        };
    }

    internal static class Settings
    {
        public static readonly LocString Title = new LocString("settings.title", "Settings");

        public static readonly LocString Language = new LocString("settings.language", "Language");

        public static readonly LocString LanguageHelp = new LocString("settings.languageHelp", "The language of this plugin's windows. Card, NPC, and zone names always follow the game client.");

        public static readonly LocString SearchHint = new LocString("settings.searchHint", "Search...");

        public static readonly LocString CatGeneral = new LocString("settings.cat.general", "General");

        public static readonly LocString CatGeneralSub = new LocString("settings.cat.generalSub", "Window and behavior preferences.");

        public static readonly LocString GeneralWindow = new LocString("settings.general.window", "Window");

        public static readonly LocString OpenOnLogin = new LocString("settings.general.openOnLogin", "Open on login");

        public static readonly LocString OpenOnLoginHelp = new LocString("settings.general.openOnLoginHelp", "Pop the main window automatically the next time you log in.");

        public static readonly LocString DragAnywhere = new LocString("settings.general.dragAnywhere", "Drag from anywhere");

        public static readonly LocString DragAnywhereHelp = new LocString("settings.general.dragAnywhereHelp", "Move the window by dragging anywhere inside it. When off, only the top bar moves it.");

        public static readonly LocString GeneralBehavior = new LocString("settings.general.behavior", "Behavior");

        public static readonly LocString AutoPause = new LocString("settings.general.autoPause", "Auto-pause in content");

        public static readonly LocString AutoPauseHelp = new LocString("settings.general.autoPauseHelp", "Pause the run while you are inside a duty, trial, raid, or any other instanced content, then resume it once you are back outside. Your plan and session stats are kept.");

        public static readonly LocString PostRunGroup = new LocString("settings.postRun.group", "Chat commands");

        public static readonly LocString PostRunLabel = new LocString("settings.postRun.label", "Run when the run finishes");

        public static readonly LocString PostRunHelp = new LocString("settings.postRun.help", "One command per line, each starting with a slash. They run when a run ends on its own, before the after-run action, and never after Stop or a failure.");

        public static readonly LocString PostRunCaption = new LocString("settings.postRun.caption", "Lines that do not start with a slash are skipped, so nothing is ever said in chat. Try /li home to head home or /ays m to hand off to AutoRetainer.");
    }

    internal static class Plugin
    {
        public static readonly LocString CommandHelp = new LocString("plugin.commandHelp", "Toggle the Triple Triad Autopilot window. /tta config | stats | deps | log | changelog | about | pause (pause or resume the run) | npcs (write the Triple Triad NPC table to the plugin log) | board | request (print what the plugin reads off the open match windows).");

        public static readonly LocString CommandHelpAlias = new LocString("plugin.commandHelpAlias", "Alias for /tta.");
    }

    internal static class Deck
    {
        public static readonly LocString Category = new LocString("deck.cat", "Deck");

        public static readonly LocString CategorySub = new LocString("deck.catSub", "Which deck each match is played with.");

        public static readonly LocString GroupSource = new LocString("deck.group.source", "Deck");

        public static readonly LocString Source = new LocString("deck.source", "Deck to play");

        public static readonly LocString SourceHelp = new LocString("deck.sourceHelp", "An optimized deck is built for every NPC from the cards you own and that NPC's rules. Your own deck is used as it is.");

        public static readonly LocString SourceOptimized = new LocString("deck.source.optimized", "Optimized per NPC");

        public static readonly LocString SourceOptimizedDetail = new LocString("deck.source.optimizedDetail", "Build the strongest deck against each NPC from your collection.");

        public static readonly LocString SourceOwn = new LocString("deck.source.own", "My own deck");

        public static readonly LocString SourceOwnDetail = new LocString("deck.source.ownDetail", "Always play one of your saved decks.");

        public static readonly LocString OwnSlot = new LocString("deck.ownSlot", "Saved deck");

        public static readonly LocString OwnSlotHelp = new LocString("deck.ownSlotHelp", "The saved deck picked at the start of every match.");

        public static readonly LocString WriteSlot = new LocString("deck.writeSlot", "Deck slot to use");

        public static readonly LocString WriteSlotHelp = new LocString("deck.writeSlotHelp", "The optimized deck is put into this saved deck slot before each match.");

        public static readonly LocString WriteSlotWarning = new LocString("deck.writeSlotWarning", "The cards in this slot are replaced while the plugin plays. Pick a slot you do not use.");

        public static readonly LocString SlotLabel = new LocString("deck.slotLabel", "Deck {0}");

        public static readonly LocString GroupOptimizer = new LocString("deck.group.optimizer", "Optimizer");

        public static readonly LocString Timeout = new LocString("deck.timeout", "Time limit");

        public static readonly LocString TimeoutHelp = new LocString("deck.timeoutHelp", "How long the optimizer may search for a deck. The best deck found so far is used when time runs out.");

        public static readonly LocString SecondsFormat = new LocString("deck.secondsFormat", "%d s");

        public static readonly LocString Threads = new LocString("deck.threads", "Worker threads");

        public static readonly LocString ThreadsHelp = new LocString("deck.threadsHelp", "How many CPU threads the optimizer may use. Auto uses a quarter of them so the game keeps running smoothly.");

        public static readonly LocString ThreadsAuto = new LocString("deck.threadsAuto", "Auto");
    }

    internal static class Play
    {
        public static readonly LocString Category = new LocString("play.cat", "Play");

        public static readonly LocString CategorySub = new LocString("play.catSub", "How long to stay with an NPC and when to move on.");

        public static readonly LocString GroupNpcs = new LocString("play.group.npcs", "Matches");

        public static readonly LocString LossStreak = new LocString("play.lossStreak", "Move on after losses in a row");

        public static readonly LocString LossStreakHelp = new LocString("play.lossStreakHelp", "Skip an NPC after this many losses in a row and come back to it on a later run.");

        public static readonly LocString Never = new LocString("play.never", "never");

        public static readonly LocString MatchLimit = new LocString("play.matchLimit", "Most matches per NPC");

        public static readonly LocString MatchLimitHelp = new LocString("play.matchLimitHelp", "Collect mode moves on after this many matches, even if a card has not dropped yet.");

        public static readonly LocString NoLimit = new LocString("play.noLimit", "no limit");

        public static readonly LocString MaxFee = new LocString("play.maxFee", "Highest match fee");

        public static readonly LocString MaxFeeHelp = new LocString("play.maxFeeHelp", "NPCs asking more MGP per match than this are left out of the plan.");

        public static readonly LocString AnyFee = new LocString("play.anyFee", "any fee");

        public static readonly LocString MgpFormat = new LocString("play.mgpFormat", "%d MGP");

        public static readonly LocString Delay = new LocString("play.delay", "Pause between matches");

        public static readonly LocString DelayHelp = new LocString("play.delayHelp", "Wait this long after a match before asking for the next one.");

        public static readonly LocString MillisecondsFormat = new LocString("play.msFormat", "%d ms");

        public static readonly LocString GroupInventory = new LocString("play.group.inventory", "Inventory");

        public static readonly LocString FreeSlots = new LocString("play.freeSlots", "Free bag slots to keep");

        public static readonly LocString FreeSlotsHelp = new LocString("play.freeSlotsHelp", "Won cards land in your bags. Below this many free slots, new cards are registered first, and the run pauses if that does not free enough room.");
    }

    internal static class Safety
    {
        public static readonly LocString Add = new LocString("safety.add", "Add");

        public static readonly LocString Remove = new LocString("safety.remove", "Remove");

        public static readonly LocString Test = new LocString("safety.test", "Test");

        public static readonly LocString Preview = new LocString("safety.preview", "Preview");

        public static readonly LocString CatPartyInvites = new LocString("safety.cat.partyInvites", "Party invites");

        public static readonly LocString CatPartyInvitesSub = new LocString("safety.cat.partyInvitesSub", "Auto-decline incoming party invites during a run, after a human-like delay, with an optional reply.");

        public static readonly LocString CatGmAlert = new LocString("safety.cat.gmAlert", "GM alert");

        public static readonly LocString CatGmAlertSub = new LocString("safety.cat.gmAlertSub", "Detects nearby Game Masters and reacts: stop the bot, ping you, or take more drastic action.");

        public static readonly LocString SecondsFormat = new LocString("safety.humanizer.secondsFormat", "%d s");

        public static readonly LocString InvitesDecline = new LocString("safety.invites.decline", "Decline");

        public static readonly LocString AutoDecline = new LocString("safety.invites.autoDecline", "Auto-decline party invites");

        public static readonly LocString AutoDeclineHelp = new LocString("safety.invites.autoDeclineHelp", "While a run is going, automatically decline incoming party invites after a short random delay. Invites that arrive while idle or playing manually are left alone for you to handle.");

        public static readonly LocString AutoDeclineOff = new LocString("safety.invites.autoDeclineOff", "Auto-decline is off. Enable it to configure it.");

        public static readonly LocString DeclineDelay = new LocString("safety.invites.delay", "Decline delay");

        public static readonly LocString DeclineDelayHelp = new LocString("safety.invites.delayHelp", "Wait a random time in this range before declining, so it looks like you noticed the popup and dismissed it yourself.");

        public static readonly LocString InvitesReply = new LocString("safety.invites.reply", "Reply");

        public static readonly LocString SendReply = new LocString("safety.invites.sendReply", "Send a reply");

        public static readonly LocString SendReplyHelp = new LocString("safety.invites.sendReplyHelp", "After declining, send a chat message so it reads like a polite human brush-off rather than an instant silent decline.");

        public static readonly LocString ReplyChannel = new LocString("safety.invites.channel", "Reply channel");

        public static readonly LocString ReplyChannelHelp = new LocString("safety.invites.channelHelp", "Where the message goes. \"Tell inviter\" whispers the person who invited you. Ignored when your message starts with a slash command.");

        public static readonly LocString ChannelTellName = new LocString("safety.invites.channelTell.name", "Tell inviter");

        public static readonly LocString ChannelTellDetail = new LocString("safety.invites.channelTell.detail", "Whisper the person who invited you.");

        public static readonly LocString ChannelSayName = new LocString("safety.invites.channelSay.name", "Say");

        public static readonly LocString ChannelSayDetail = new LocString("safety.invites.channelSay.detail", "Local /say, heard by players near you.");

        public static readonly LocString ChannelYellName = new LocString("safety.invites.channelYell.name", "Yell");

        public static readonly LocString ChannelYellDetail = new LocString("safety.invites.channelYell.detail", "Zone-wide /yell.");

        public static readonly LocString ReplyMessage = new LocString("safety.invites.message", "Reply message");

        public static readonly LocString ReplyMessageHelp = new LocString("safety.invites.messageHelp", "Use {name} for the inviter's character name and {world} for their home world. If the message begins with \"/\", it's sent verbatim as a command (e.g. /tell {name}@{world} busy right now!).");

        public static readonly LocString ReplyMessageHint = new LocString("safety.invites.messageHint", "Sorry {name}, I'm busy right now!");

        public static readonly LocString GmAlerts = new LocString("safety.gm.alerts", "Alerts");

        public static readonly LocString GmStopRun = new LocString("safety.gm.stopRun", "Stop the run");

        public static readonly LocString GmStopRunHelp = new LocString("safety.gm.stopRunHelp", "Halt automation immediately when a GM appears nearby. Strongly recommended; the rest of the alerts are useless if the bot keeps playing.");

        public static readonly LocString GmToast = new LocString("safety.gm.toast", "Toast notification");

        public static readonly LocString GmToastHelp = new LocString("safety.gm.toastHelp", "Pop a Dalamud toast: \"GM <name> is nearby!\"");

        public static readonly LocString GmChat = new LocString("safety.gm.chat", "Chat alert");

        public static readonly LocString GmChatHelp = new LocString("safety.gm.chatHelp", "Print a red chat warning into your local log.");

        public static readonly LocString GmSound = new LocString("safety.gm.sound", "Sound beeps");

        public static readonly LocString GmSoundHelp = new LocString("safety.gm.soundHelp", "Plays a series of system beeps through your speakers. Loud enough to grab your attention if you're tabbed away.");

        public static readonly LocString BeepCount = new LocString("safety.gm.beepCount", "Beep count");

        public static readonly LocString BeepCountHelp = new LocString("safety.gm.beepCountHelp", "How many beeps to play in the burst.");

        public static readonly LocString BeepCountFormat = new LocString("safety.gm.beepCountFormat", "%d beeps");

        public static readonly LocString BeepLength = new LocString("safety.gm.beepLength", "Beep length");

        public static readonly LocString BeepLengthHelp = new LocString("safety.gm.beepLengthHelp", "How long each beep lasts.");

        public static readonly LocString BeepLengthFormat = new LocString("safety.gm.beepLengthFormat", "%d ms each");

        public static readonly LocString BeepPitch = new LocString("safety.gm.beepPitch", "Beep pitch");

        public static readonly LocString BeepPitchHelp = new LocString("safety.gm.beepPitchHelp", "Tone frequency of each beep.");

        public static readonly LocString BeepPitchFormat = new LocString("safety.gm.beepPitchFormat", "%d Hz");

        public static readonly LocString GmActions = new LocString("safety.gm.actions", "Actions");

        public static readonly LocString GmCommands = new LocString("safety.gm.commands", "Custom commands");

        public static readonly LocString GmCommandsHelp = new LocString("safety.gm.commandsHelp", "Chat commands to run when a GM is spotted. Useful for things like /logout, /sh stay calm, or a macro.");

        public static readonly LocString GmKill = new LocString("safety.gm.kill", "Kill the game");

        public static readonly LocString GmKillHelp = new LocString("safety.gm.killHelp", "Hard-terminate the game process via /xlkill. The last-resort option; no goodbyes, no cutscene, no logout. You'll get a disconnect.");

        public static readonly LocString NoCommands = new LocString("safety.gm.noCommands", "No commands queued.");
    }

    internal static class Session
    {
        public static readonly LocString AutoResume = new LocString("session.autoResume", "Auto-resume on fault");

        public static readonly LocString AutoResumeHelp = new LocString("session.autoResumeHelp", "If the run hits an unexpected error and stops, restart it automatically (up to 3 times in 5 minutes) instead of ending the run. Turn it off if you would rather have faults end the run.");
    }
}

internal enum PluralKind : byte
{
    EnglishLike,
    French
}
