using System;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class PartyInviteSettings
{
    private const int DelaySecondsMax = 30;

    private const float MessageInputWidth = 360f;

    private const int MessageMaxLength = 480;

    private static readonly SettingsControls.Choices.Choice[] channelChoices = new SettingsControls.Choices.Choice[3]
    {
        new SettingsControls.Choices.Choice(L.Safety.ChannelTellName, L.Safety.ChannelTellDetail),
        new SettingsControls.Choices.Choice(L.Safety.ChannelSayName, L.Safety.ChannelSayDetail),
        new SettingsControls.Choices.Choice(L.Safety.ChannelYellName, L.Safety.ChannelYellDetail)
    };

    public static void Draw(Configuration configuration)
    {
        DrawDeclineGroup(configuration);
        using var replySection = Motion.PushSection("##tta_invites_reply", configuration.DeclinePartyInvites);
        if (replySection == null)
        {
            return;
        }
        DrawReplyGroup(configuration);
    }

    private static void DrawDeclineGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Safety.InvitesDecline)))
        {
            SettingsRow.Draw(Loc.T(L.Safety.AutoDecline), Loc.T(L.Safety.AutoDeclineHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.DeclinePartyInvites, (bool value) =>
                {
                    configuration.DeclinePartyInvites = value;
                }, "##tta_invites_on");
            }, SettingsRow.ToggleHeight);
            using var bodySwitch = Motion.PushSwitch("##tta_invites_body", configuration.DeclinePartyInvites);
            if (!configuration.DeclinePartyInvites)
            {
                SettingsRow.Note(Loc.T(L.Safety.AutoDeclineOff));
                return;
            }
            SettingsRow.Draw(Loc.T(L.Safety.DeclineDelay), Loc.T(L.Safety.DeclineDelayHelp), SettingsControls.RangeInlineWidth(), () =>
            {
                SettingsControls.DrawRangeInline(configuration, "##tta_invites_delay_min", "##tta_invites_delay_max", () => configuration.DeclineInviteDelayMinSeconds, (int value) =>
                {
                    configuration.DeclineInviteDelayMinSeconds = value;
                }, () => configuration.DeclineInviteDelayMaxSeconds, (int value) =>
                {
                    configuration.DeclineInviteDelayMaxSeconds = value;
                }, DelaySecondsMax, 0, Loc.T(L.Safety.SecondsFormat));
            });
        }
    }

    private static void DrawReplyGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Safety.InvitesReply));
        SettingsRow.Draw(Loc.T(L.Safety.SendReply), Loc.T(L.Safety.SendReplyHelp), SettingsControls.ToggleWidth, () =>
        {
            SettingsControls.DrawToggle(configuration, () => configuration.DeclineInviteReply, (bool value) =>
            {
                configuration.DeclineInviteReply = value;
            }, "##tta_invites_reply_on");
        }, SettingsRow.ToggleHeight);
        using var channelSection = Motion.PushSection("##tta_invites_channel_section", configuration.DeclineInviteReply);
        if (channelSection == null)
        {
            return;
        }
        var selected = Math.Clamp((int)configuration.DeclineInviteReplyChannel, 0, channelChoices.Length - 1);
        SettingsRow.Draw(Loc.T(L.Safety.ReplyChannel), Loc.T(L.Safety.ReplyChannelHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##tta_invites_channel", channelChoices, selected, (int choice) =>
            {
                configuration.DeclineInviteReplyChannel = (PartyInviteReplyChannel)choice;
                configuration.SaveDebounced();
            });
        });
        SettingsRow.Caption(Loc.T(channelChoices[selected].Detail));
        SettingsRow.DrawBlock(Loc.T(L.Safety.ReplyMessage), Loc.T(L.Safety.ReplyMessageHelp), () =>
        {
            DrawMessageInput(configuration);
        });
    }

    private static void DrawMessageInput(Configuration configuration)
    {
        var message = configuration.DeclineInviteReplyMessage;
        ImGui.SetNextItemWidth(MessageInputWidth * ImGuiHelpers.GlobalScale);
        bool changed;
        using (SettingsControls.PushFrameColors())
        {
            changed = ImGui.InputTextWithHint("##tta_invites_message", Loc.T(L.Safety.ReplyMessageHint), ref message, MessageMaxLength);
        }
        if (!changed)
        {
            return;
        }
        configuration.DeclineInviteReplyMessage = message;
        configuration.SaveDebounced();
    }
}
