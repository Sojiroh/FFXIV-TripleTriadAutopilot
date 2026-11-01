using AutoTripleTriadGrind.Core.Game.Watchers;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class GmAlertSettings
{
    private const int BeepCountMin = 1;

    private const int BeepCountMax = 20;

    private const int BeepLengthMinMs = 50;

    private const int BeepLengthMaxMs = 1000;

    private const int BeepPitchMinHz = 100;

    private const int BeepPitchMaxHz = 5000;

    private const float CommandInputWidth = 360f;

    private const int CommandMaxLength = 200;

    private const string CommandHint = "/logout";

    private const string CommandListId = "##tta_gm_commands";

    private static string commandDraft = string.Empty;

    public static void Draw(Configuration configuration)
    {
        DrawAlertsGroup(configuration);
        DrawActionsGroup(configuration);
    }

    private static void DrawAlertsGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Safety.GmAlerts)))
        {
            SettingsRow.Draw(Loc.T(L.Safety.GmStopRun), Loc.T(L.Safety.GmStopRunHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.GmAlertStopRun, (bool value) =>
                {
                    configuration.GmAlertStopRun = value;
                }, "##tta_gm_stop");
            }, SettingsRow.ToggleHeight);
            SettingsRow.Draw(Loc.T(L.Safety.GmToast), Loc.T(L.Safety.GmToastHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.GmAlertToast, (bool value) =>
                {
                    configuration.GmAlertToast = value;
                }, "##tta_gm_toast");
            }, SettingsRow.ToggleHeight);
            SettingsRow.Draw(Loc.T(L.Safety.GmChat), Loc.T(L.Safety.GmChatHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.GmAlertChat, (bool value) =>
                {
                    configuration.GmAlertChat = value;
                }, "##tta_gm_chat");
            }, SettingsRow.ToggleHeight);
            SettingsRow.Draw(Loc.T(L.Safety.GmSound), Loc.T(L.Safety.GmSoundHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.GmAlertSound, (bool value) =>
                {
                    configuration.GmAlertSound = value;
                }, "##tta_gm_sound");
            }, SettingsRow.ToggleHeight);
            using var beepSection = Motion.PushSection("##tta_gm_beep", configuration.GmAlertSound);
            if (beepSection == null)
            {
                return;
            }
            DrawBeepRows(configuration);
        }
    }

    private static void DrawBeepRows(Configuration configuration)
    {
        SettingsRow.Draw(Loc.T(L.Safety.BeepCount), Loc.T(L.Safety.BeepCountHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(configuration, "##tta_gm_beep_count", () => configuration.GmAlertBeepCount, (int value) =>
            {
                configuration.GmAlertBeepCount = value;
            }, BeepCountMin, BeepCountMax, Loc.T(L.Safety.BeepCountFormat));
        });
        SettingsRow.Draw(Loc.T(L.Safety.BeepLength), Loc.T(L.Safety.BeepLengthHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(configuration, "##tta_gm_beep_length", () => configuration.GmAlertBeepDurationMs, (int value) =>
            {
                configuration.GmAlertBeepDurationMs = value;
            }, BeepLengthMinMs, BeepLengthMaxMs, Loc.T(L.Safety.BeepLengthFormat));
        });
        SettingsRow.Draw(Loc.T(L.Safety.BeepPitch), Loc.T(L.Safety.BeepPitchHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(configuration, "##tta_gm_beep_pitch", () => configuration.GmAlertBeepFrequencyHz, (int value) =>
            {
                configuration.GmAlertBeepFrequencyHz = value;
            }, BeepPitchMinHz, BeepPitchMaxHz, Loc.T(L.Safety.BeepPitchFormat));
        });
        SettingsRow.DrawBlock(Loc.T(L.Safety.Test), null, () =>
        {
            DrawBeepPreview(configuration);
        });
    }

    private static void DrawBeepPreview(Configuration configuration)
    {
        ImGui.PushID("##tta_gm_beep_preview");
        bool clicked;
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
        {
            clicked = ImGui.SmallButton(Loc.T(L.Safety.Preview));
        }
        ImGui.PopID();
        if (clicked)
        {
            GmAlertWatcher.PlayBeeps(configuration.GmAlertBeepCount, configuration.GmAlertBeepFrequencyHz, configuration.GmAlertBeepDurationMs);
        }
    }

    private static void DrawActionsGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Safety.GmActions)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Safety.GmCommands), Loc.T(L.Safety.GmCommandsHelp), () =>
            {
                DrawCommandList(configuration);
            });
            SettingsRow.Draw(Loc.T(L.Safety.GmKill), Loc.T(L.Safety.GmKillHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.GmAlertKillGame, (bool value) =>
                {
                    configuration.GmAlertKillGame = value;
                }, "##tta_gm_kill");
            }, SettingsRow.ToggleHeight);
        }
    }

    private static void DrawCommandList(Configuration configuration)
    {
        DrawCommandInput(configuration);
        var gmAlertCommands = configuration.GmAlertCommands;
        if (gmAlertCommands.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Safety.NoCommands));
            return;
        }
        var removeIndex = -1;
        for (var i = 0; i < gmAlertCommands.Count; i++)
        {
            ListRows.Ordinal(i);
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            {
                ImGui.TextUnformatted(gmAlertCommands[i]);
            }
            if (ListRows.RemoveButton(CommandListId, i, Loc.T(L.Safety.Remove)))
            {
                removeIndex = i;
            }
        }
        if (removeIndex < 0)
        {
            return;
        }
        gmAlertCommands.RemoveAt(removeIndex);
        configuration.SaveDebounced();
    }

    private static void DrawCommandInput(Configuration configuration)
    {
        var draft = commandDraft;
        bool submitted;
        using (SettingsControls.PushFrameColors())
        {
            ImGui.SetNextItemWidth(CommandInputWidth * ImGuiHelpers.GlobalScale);
            submitted = ImGui.InputTextWithHint("##tta_gm_command_input", CommandHint, ref draft, CommandMaxLength, ImGuiInputTextFlags.EnterReturnsTrue);
        }
        commandDraft = draft;
        ImGui.SameLine();
        ImGui.PushID("##tta_gm_command_add");
        bool addClicked;
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
        {
            addClicked = ImGui.SmallButton(Loc.T(L.Safety.Add));
        }
        ImGui.PopID();
        if (!submitted && !addClicked)
        {
            return;
        }
        AddCommand(configuration, commandDraft);
        commandDraft = string.Empty;
    }

    private static void AddCommand(Configuration configuration, string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }
        var command = (trimmed.StartsWith('/') ? trimmed : ("/" + trimmed));
        if (configuration.GmAlertCommands.Contains(command))
        {
            return;
        }
        configuration.GmAlertCommands.Add(command);
        configuration.SaveDebounced();
    }
}
