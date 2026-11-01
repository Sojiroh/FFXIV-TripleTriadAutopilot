using System;
using System.Numerics;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class PostRunSettings
{
    private const int MaxLength = 1000;

    private const int MinLines = 3;

    private const int MaxLines = 8;

    public static void Draw(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.PostRunGroup)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.PostRunLabel), Loc.T(L.Settings.PostRunHelp), () =>
            {
                DrawEditor(configuration);
            });
            SettingsRow.Caption(Loc.T(L.Settings.PostRunCaption));
        }
    }

    private static void DrawEditor(Configuration configuration)
    {
        var commands = configuration.PostRunCommands;
        var lineCount = 1;
        for (var i = 0; i < commands.Length; i++)
        {
            if (commands[i] == '\n')
            {
                lineCount++;
            }
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = ImGui.GetTextLineHeight() * (float)Math.Clamp(lineCount + 1, MinLines, MaxLines) + ImGui.GetStyle().FramePadding.Y * 2f;
        var width = SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X;
        using var frameColors = SettingsControls.PushFrameColors();
        using var frameStyle = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f).Push(ImGuiStyleVar.FrameRounding, Styling.FrameRounding * globalScale);
        if (!ImGui.InputTextMultiline("##tta_general_postrun", ref commands, MaxLength, new Vector2(width, height)))
        {
            return;
        }
        configuration.PostRunCommands = commands;
        configuration.SaveDebounced();
    }
}
