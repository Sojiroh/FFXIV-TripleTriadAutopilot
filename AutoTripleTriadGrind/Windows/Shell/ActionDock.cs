using System.Numerics;
using AutoTripleTriadGrind.Core.External;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;

    private const float ButtonGap = 8f;

    public static void Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var end = cursorScreenPos + size;
        Dock.Background(ImGui.GetWindowDrawList(), cursorScreenPos, end, windowRounding);
        var padding = PadX * globalScale;
        var buttonHeight = Layout.HeroButtonHeight * globalScale;
        var innerWidth = size.X - padding * 2f;
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + padding, cursorScreenPos.Y + (size.Y - buttonHeight) * 0.5f));
        if (plugin.Controller.Running)
        {
            DrawRunControls(plugin, innerWidth);
        }
        else
        {
            DrawStart(plugin, innerWidth);
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
    }

    private static void DrawRunControls(Plugin plugin, float innerWidth)
    {
        var controller = plugin.Controller;
        var gap = ButtonGap * ImGuiHelpers.GlobalScale;
        var width = (innerWidth - gap) * 0.5f;
        if (PauseButton.Draw(controller.PauseReason, width))
        {
            controller.TogglePause();
        }
        ImGui.SameLine(0f, gap);
        var sessionSnapshot = controller.SessionSnapshot;
        var stateLabel = (controller.Paused ? Loc.T(L.Triad.StatePaused) : Loc.T(L.Triad.StateRunning));
        if (StopButton.Draw((sessionSnapshot == null) ? stateLabel : Loc.T(L.Triad.StopSub, stateLabel, Formatting.Elapsed(sessionSnapshot.Elapsed)), width))
        {
            controller.Stop();
        }
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var assessment = TriadLauncher.Assess(plugin.Configuration);
        var pluginsInstalled = ExternalPlugins.AllRequiredInstalled();
        var enabled = (assessment.Readiness == TriadLauncher.Readiness.Ready) & pluginsInstalled;
        var disabledReason = ((!pluginsInstalled) ? Loc.T(L.Triad.ReasonInstall) : TriadLauncher.Reason(in assessment));
        if (StartButton.Draw(TriadLauncher.Sublabel(in assessment), enabled, disabledReason, innerWidth))
        {
            TriadLauncher.Start(assessment.Mode);
        }
    }
}
