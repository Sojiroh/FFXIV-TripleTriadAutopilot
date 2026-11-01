using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AutoTripleTriadGrind.Core;
using AutoTripleTriadGrind.Core.Debug;
using AutoTripleTriadGrind.Core.Game.Watchers;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Stats;
using AutoTripleTriadGrind.Core.Tasks;
using AutoTripleTriadGrind.Windows;
using AutoTripleTriadGrind.Windows.Shell;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using clib;

namespace AutoTripleTriadGrind;

public sealed class Plugin : IDalamudPlugin, IDisposable
{
    private const string GotoSubcommand = "goto";

    private const string NavmeshIpcProviderMarker = "Navmesh.IPCProvider";

    private readonly DutyWatcher dutyWatcher;

    private readonly GmAlertWatcher gmAlertWatcher;

    private readonly PartyInviteWatcher partyInviteWatcher;

    private readonly AppWindow appWindow;

    private readonly CommandInfo primaryCommand;

    private readonly CommandInfo aliasCommand;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    internal static Plugin Instance { get; private set; } = null!;

    internal Configuration Configuration { get; }

    internal WindowSystem WindowSystem { get; } = new WindowSystem("TripleTriadAutopilot");

    internal RunHistory History { get; }

    internal AutoTriadController Controller { get; }

    private static string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;

    public Plugin()
    {
        Instance = this;
        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Configuration = (PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
        History = new RunHistory();
        Controller = new AutoTriadController();
        dutyWatcher = new DutyWatcher();
        gmAlertWatcher = new GmAlertWatcher();
        partyInviteWatcher = new PartyInviteWatcher();
        InitializeLocalization();
        Fonts.Initialize(PluginInterface.UiBuilder, PluginDirectory);
        appWindow = new AppWindow(this);
        WindowSystem.AddWindow(appWindow);
        primaryCommand = new CommandInfo(OnCommand)
        {
            HelpMessage = Loc.T(L.Plugin.CommandHelp)
        };
        aliasCommand = new CommandInfo(OnCommand)
        {
            HelpMessage = Loc.T(L.Plugin.CommandHelpAlias)
        };
        CommandManager.AddHandler(AttgConstants.PrimaryCommand, primaryCommand);
        CommandManager.AddHandler(AttgConstants.AliasCommand, aliasCommand);
        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        Svc.ClientState.Login += OnLogin;
        if (Svc.ClientState.IsLoggedIn)
        {
            OnLogin();
        }
    }

    public void Dispose()
    {
        Controller.Stop();
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        Svc.ClientState.Login -= OnLogin;
        Configuration.SaveIfPending();
        WindowSystem.RemoveAllWindows();
        appWindow.Dispose();
        Fonts.Dispose();
        CommandManager.RemoveHandler(AttgConstants.PrimaryCommand);
        CommandManager.RemoveHandler(AttgConstants.AliasCommand);
        dutyWatcher.Dispose();
        gmAlertWatcher.Dispose();
        partyInviteWatcher.Dispose();
        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    public void OnLanguageChanged()
    {
        primaryCommand.HelpMessage = Loc.T(L.Plugin.CommandHelp);
        aliasCommand.HelpMessage = Loc.T(L.Plugin.CommandHelpAlias);
    }

    public void ToggleMainUi()
    {
        appWindow.Toggle();
    }

    public void ToggleConfigUi()
    {
        appWindow.TogglePage(AppWindow.Page.Settings);
    }

    public void ToggleAboutUi()
    {
        appWindow.TogglePage(AppWindow.Page.About);
    }

    public void ToggleDependenciesUi()
    {
        appWindow.TogglePage(AppWindow.Page.Plugins);
    }

    public void ToggleHistoryUi()
    {
        appWindow.TogglePage(AppWindow.Page.History);
    }

    public void ToggleLogUi()
    {
        appWindow.TogglePage(AppWindow.Page.Log);
    }

    public void ToggleChangelogUi()
    {
        appWindow.TogglePage(AppWindow.Page.Changelog);
    }

    private void OnCommand(string command, string args)
    {
        var subcommand = args.Trim();
        if (subcommand.Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            ToggleConfigUi();
        }
        else if (subcommand.Equals("about", StringComparison.OrdinalIgnoreCase))
        {
            ToggleAboutUi();
        }
        else if (subcommand.Equals("deps", StringComparison.OrdinalIgnoreCase) || subcommand.Equals("dependencies", StringComparison.OrdinalIgnoreCase))
        {
            ToggleDependenciesUi();
        }
        else if (subcommand.Equals("stats", StringComparison.OrdinalIgnoreCase) || subcommand.Equals("history", StringComparison.OrdinalIgnoreCase))
        {
            ToggleHistoryUi();
        }
        else if (subcommand.Equals("log", StringComparison.OrdinalIgnoreCase))
        {
            ToggleLogUi();
        }
        else if (subcommand.Equals("changelog", StringComparison.OrdinalIgnoreCase))
        {
            ToggleChangelogUi();
        }
        else if (subcommand.Equals("pause", StringComparison.OrdinalIgnoreCase) || subcommand.Equals("resume", StringComparison.OrdinalIgnoreCase))
        {
            Controller.TogglePause();
        }
        else if (subcommand.Equals("npcs", StringComparison.OrdinalIgnoreCase))
        {
            TriadNpcDumper.Dump();
        }
        else if (subcommand.Equals("board", StringComparison.OrdinalIgnoreCase))
        {
            TriadBoardDumper.DumpBoard();
        }
        else if (subcommand.Equals("request", StringComparison.OrdinalIgnoreCase))
        {
            TriadBoardDumper.DumpRequest();
        }
        else if (IsGotoCommand(subcommand))
        {
            AutoGoto.HandleCommand(subcommand.Substring(GotoSubcommand.Length).Trim(), Controller.Running);
        }
        else
        {
            ToggleMainUi();
        }
    }

    private void OnDraw()
    {
        WindowSystem.Draw();
        Configuration.FlushPendingSave();
    }

    private static bool IsGotoCommand(string arguments)
    {
        if (arguments.StartsWith(GotoSubcommand, StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Length != GotoSubcommand.Length)
            {
                return char.IsWhiteSpace(arguments[GotoSubcommand.Length]);
            }
            return true;
        }
        return false;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        if (eventArgs.Observed)
        {
            return;
        }
        if (!eventArgs.Exception.ToString().Contains(NavmeshIpcProviderMarker, StringComparison.Ordinal))
        {
            return;
        }
        eventArgs.SetObserved();
        RunLog.Debug("Observed a navmesh IPC task fault: " + eventArgs.Exception.GetBaseException().Message);
    }

    private void InitializeLocalization()
    {
        var localizationDirectory = Path.Combine(PluginDirectory, "Localization");
        if (string.IsNullOrEmpty(Configuration.Language))
        {
            Configuration.Language = DetectLanguage();
            Configuration.Save();
        }
        Loc.Initialize(Configuration.Language, localizationDirectory);
    }

    private static string DetectLanguage()
    {
        var uiLanguage = PluginInterface.UiLanguage;
        if (Languages.IsKnown(uiLanguage))
        {
            return Languages.Resolve(uiLanguage).Code;
        }
        string systemLanguage;
        switch (Svc.ClientState.ClientLanguage)
        {
            case ClientLanguage.German:
                return Languages.German.Code;
            case ClientLanguage.French:
                return Languages.French.Code;
            case ClientLanguage.Japanese:
                return Languages.Japanese.Code;
            default:
                systemLanguage = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                if (!Languages.IsKnown(systemLanguage))
                {
                    return Languages.English.Code;
                }
                return Languages.Resolve(systemLanguage).Code;
        }
    }

    private void OnLogin()
    {
        if (!Configuration.AutoShowOnLogin)
        {
            return;
        }
        appWindow.Show(AppWindow.Page.Triad);
    }
}

public enum PartyInviteReplyChannel
{
    Tell,
    Say,
    Yell
}
