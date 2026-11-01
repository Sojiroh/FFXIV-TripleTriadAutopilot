using System;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace AutoTripleTriadGrind.Core.Game.Watchers;

internal sealed class PartyInviteWatcher : IDisposable
{
    private enum Stage : byte
    {
        Idle,
        DeclinePending,
        ConfirmPending
    }

    private readonly struct PromptTemplate
    {
        public static readonly PromptTemplate Invalid = new PromptTemplate("", "");

        private readonly string prefix;

        private readonly string suffix;

        public bool IsValid => prefix.Length + suffix.Length > 0;

        private PromptTemplate(string prefix, string suffix)
        {
            this.prefix = prefix;
            this.suffix = suffix;
        }

        public bool Matches(string prompt)
        {
            if (IsValid && prompt.Length > prefix.Length + suffix.Length && prompt.StartsWith(prefix, StringComparison.Ordinal))
            {
                return prompt.EndsWith(suffix, StringComparison.Ordinal);
            }
            return false;
        }

        public override string ToString()
        {
            return prefix + "<name>" + suffix;
        }

        public static PromptTemplate FromAddonRow(uint rowId)
        {
            var row = Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(rowId);
            if (!row.HasValue)
            {
                return Invalid;
            }
            var prefixBuilder = new StringBuilder();
            var suffixBuilder = new StringBuilder();
            var afterPlaceholder = false;
            foreach (var item in row.Value.Text)
            {
                var payload = item;
                if (payload.Type == ReadOnlySePayloadType.Text)
                {
                    (afterPlaceholder ? suffixBuilder : prefixBuilder).Append(Encoding.UTF8.GetString(payload.Body.Span));
                }
                else if (!IsLayoutMacro(in payload))
                {
                    afterPlaceholder = true;
                    suffixBuilder.Clear();
                }
            }
            if (!afterPlaceholder)
            {
                return Invalid;
            }
            return new PromptTemplate(Normalize(prefixBuilder.ToString()), Normalize(suffixBuilder.ToString()));
        }

        public static string Normalize(string text)
        {
            var builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                bool skip;
                switch (c)
                {
                    case '\n':
                    case '\r':
                    case '\u00a0':
                    case '\u00ad':
                        skip = true;
                        break;
                    default:
                        skip = false;
                        break;
                }
                if (!skip)
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        private static bool IsLayoutMacro(in ReadOnlySePayload payload)
        {
            var isLayout = payload.Type == ReadOnlySePayloadType.Macro;
            if (isLayout)
            {
                var macroCode = payload.MacroCode;
                isLayout = macroCode is MacroCode.NewLine or MacroCode.SoftHyphen or MacroCode.NonBreakingSpace;
            }
            return isLayout;
        }
    }

    private const string SelectYesnoAddonName = "SelectYesno";

    private const uint JoinPartyPromptRow = 120u;

    private const uint DeclineInvitePromptRow = 121u;

    private const int ConfirmDelayMinMs = 400;

    private const int ConfirmDelayMaxMs = 900;

    private const int ConfirmWaitMs = 4000;

    private const int NotReadyAbandonMs = 15000;

    private PromptTemplate joinPrompt = PromptTemplate.Invalid;

    private PromptTemplate declinePrompt = PromptTemplate.Invalid;

    private bool templatesLoaded;

    private Stage stage;

    private long actAtTick;

    private long confirmDeadlineTick;

    private ushort inviteAddonId;

    private ushort confirmAddonId;

    private string inviterName = "";

    private string inviterWorld = "";

    public PartyInviteWatcher()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, SelectYesnoAddonName, OnSelectYesnoSetup);
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, SelectYesnoAddonName, OnSelectYesnoSetup);
    }

    private static bool DeclineArmed()
    {
        var instance = Plugin.Instance;
        if (instance.Configuration.DeclinePartyInvites && instance.Controller.Running)
        {
            return !instance.Controller.Paused;
        }
        return false;
    }

    private unsafe void OnSelectYesnoSetup(AddonEvent type, AddonArgs args)
    {
        var address = (AtkUnitBase*)args.Addon.Address;
        if (address == null || stage == Stage.DeclinePending)
        {
            return;
        }
        if (stage == Stage.Idle && !DeclineArmed())
        {
            return;
        }
        EnsureTemplates();
        var prompt = ReadPrompt(address);
        if (joinPrompt.Matches(prompt))
        {
            ArmDecline(address);
            return;
        }
        if (declinePrompt.Matches(prompt))
        {
            ArmConfirm(address);
            return;
        }
        if (stage == Stage.ConfirmPending || MentionsPendingInviter(prompt))
        {
            RunLog.Warning($"Party invite: SelectYesno {address->Id} looks invite-related but matched neither prompt template: \"{prompt}\" (join '{joinPrompt}', decline '{declinePrompt}').");
            return;
        }
        RunLog.Debug($"SelectYesno {address->Id} is not a party invite: \"{prompt}\"");
    }

    private unsafe void ArmDecline(AtkUnitBase* addon)
    {
        CaptureInviter();
        var configuration = Plugin.Instance.Configuration;
        var minDelaySeconds = Math.Max(0, configuration.DeclineInviteDelayMinSeconds);
        var maxDelaySeconds = Math.Max(minDelaySeconds, configuration.DeclineInviteDelayMaxSeconds);
        var delayMs = Random.Shared.Next(minDelaySeconds, maxDelaySeconds + 1) * TimeUnits.MillisecondsPerSecond;
        stage = Stage.DeclinePending;
        inviteAddonId = addon->Id;
        confirmAddonId = 0;
        actAtTick = Environment.TickCount64 + delayMs;
        RunLog.Info($"Party invite from {DisplayName()} detected; declining in ~{delayMs / TimeUnits.MillisecondsPerSecond}s.");
    }

    private unsafe void ArmConfirm(AtkUnitBase* addon)
    {
        if (stage == Stage.Idle)
        {
            CaptureInviter();
        }
        stage = Stage.ConfirmPending;
        confirmAddonId = addon->Id;
        actAtTick = Environment.TickCount64 + Random.Shared.Next(ConfirmDelayMinMs, ConfirmDelayMaxMs + 1);
        RunLog.Debug($"Party invite: decline confirmation {addon->Id} opened for {DisplayName()}; confirming shortly.");
    }

    private void OnUpdate(IFramework _)
    {
        switch (stage)
        {
            case Stage.DeclinePending:
                UpdateDecline();
                break;
            case Stage.ConfirmPending:
                UpdateConfirm();
                break;
        }
    }

    private unsafe void UpdateDecline()
    {
        var invitePrompt = FindSelectYesno(inviteAddonId);
        if (invitePrompt == null)
        {
            RunLog.Debug("Party invite: the prompt closed before the decline; standing down.");
            stage = Stage.Idle;
            return;
        }
        var tickCount = Environment.TickCount64;
        if (tickCount < actAtTick || !WaitUntilReady(invitePrompt, tickCount, "invite prompt"))
        {
            return;
        }
        stage = Stage.ConfirmPending;
        confirmAddonId = 0;
        confirmDeadlineTick = tickCount + ConfirmWaitMs;
        if (!Click(invitePrompt, yes: false, "decline click"))
        {
            stage = Stage.Idle;
        }
    }

    private unsafe void UpdateConfirm()
    {
        var tickCount = Environment.TickCount64;
        if (confirmAddonId == 0)
        {
            if (tickCount < confirmDeadlineTick)
            {
                return;
            }
            RunLog.Info("Declined the party invite from " + DisplayName() + " (no confirmation prompt appeared).");
            FinishDecline();
            return;
        }
        var confirmPrompt = FindSelectYesno(confirmAddonId);
        if (confirmPrompt == null)
        {
            RunLog.Debug("Party invite: the confirmation prompt closed before the click; standing down.");
            stage = Stage.Idle;
            return;
        }
        if (tickCount < actAtTick || !WaitUntilReady(confirmPrompt, tickCount, "decline confirmation"))
        {
            return;
        }
        if (!Click(confirmPrompt, yes: true, "confirm click"))
        {
            stage = Stage.Idle;
            return;
        }
        RunLog.Info("Declined the party invite from " + DisplayName() + ".");
        FinishDecline();
    }

    private unsafe bool WaitUntilReady(AtkUnitBase* addon, long now, string what)
    {
        if (GenericHelpers.IsAddonReady(addon))
        {
            return true;
        }
        if (now < actAtTick + NotReadyAbandonMs)
        {
            return false;
        }
        RunLog.Warning($"Party invite: {what} {addon->Id} never became ready; standing down.");
        stage = Stage.Idle;
        return false;
    }

    private void FinishDecline()
    {
        stage = Stage.Idle;
        if (Plugin.Instance.Configuration.DeclineInviteReply)
        {
            SendReply();
        }
    }

    private unsafe static AtkUnitBase* FindSelectYesno(ushort addonId)
    {
        if (addonId == 0)
        {
            return null;
        }
        var addon = RaptureAtkUnitManager.Instance()->GetAddonById(addonId);
        if (addon == null)
        {
            return null;
        }
        if (!(addon->NameString == SelectYesnoAddonName))
        {
            return null;
        }
        return addon;
    }

    private unsafe static bool Click(AtkUnitBase* addon, bool yes, string what)
    {
        try
        {
            var selectYesno = new AddonMaster.SelectYesno((nint)addon);
            if (yes)
            {
                selectYesno.Yes();
            }
            else
            {
                selectYesno.No();
            }
            return true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: " + what + " threw.");
            return false;
        }
    }

    private unsafe static string ReadPrompt(AtkUnitBase* addon)
    {
        try
        {
            var selectYesno = new AddonMaster.SelectYesno((nint)addon);
            return PromptTemplate.Normalize((selectYesno.Addon->PromptText != null) ? selectYesno.Text : selectYesno.SeStringNullTerminated.GetText()).Trim();
        }
        catch (Exception ex)
        {
            RunLog.Debug("Party invite: failed to read the SelectYesno prompt: " + ex.Message);
            return "";
        }
    }

    private void EnsureTemplates()
    {
        if (templatesLoaded)
        {
            return;
        }
        templatesLoaded = true;
        try
        {
            joinPrompt = PromptTemplate.FromAddonRow(JoinPartyPromptRow);
            declinePrompt = PromptTemplate.FromAddonRow(DeclineInvitePromptRow);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: failed to read the Addon sheet prompt templates.");
        }
        if (!joinPrompt.IsValid || !declinePrompt.IsValid)
        {
            RunLog.Warning("Party invite: prompt templates unavailable; auto-decline cannot identify invites.");
            return;
        }
        RunLog.Debug($"Party invite templates: join '{joinPrompt}', decline '{declinePrompt}'.");
    }

    private unsafe static bool MentionsPendingInviter(string prompt)
    {
        try
        {
            var invite = InfoProxyPartyInvite.Instance();
            if (invite == null)
            {
                return false;
            }
            var name = invite->InviterName.ToString();
            return name.Length > 0 && prompt.Contains(name, StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void SendReply()
    {
        var message = (Plugin.Instance.Configuration.DeclineInviteReplyMessage ?? "").Trim();
        if (message.Length == 0)
        {
            return;
        }
        message = message.Replace("{name}", inviterName).Replace("{world}", inviterWorld);
        var finalCommand = message.StartsWith('/') ? message : Plugin.Instance.Configuration.DeclineInviteReplyChannel switch
        {
            PartyInviteReplyChannel.Tell => BuildTell(message),
            PartyInviteReplyChannel.Yell => "/yell " + message,
            _ => "/say " + message,
        };
        try
        {
            Chat.SendMessage(finalCommand);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: sending the reply threw for '" + finalCommand + "'.");
        }
    }

    private string BuildTell(string body)
    {
        if (string.IsNullOrEmpty(inviterName))
        {
            return "/say " + body;
        }
        var target = (string.IsNullOrEmpty(inviterWorld) ? inviterName : (inviterName + "@" + inviterWorld));
        return "/tell " + target + " " + body;
    }

    private unsafe void CaptureInviter()
    {
        inviterName = "";
        inviterWorld = "";
        try
        {
            var invite = InfoProxyPartyInvite.Instance();
            if (invite == null)
            {
                return;
            }
            inviterName = invite->InviterName.ToString();
            var world = Svc.Data.GetExcelSheet<World>().GetRowOrDefault(invite->InviterWorldId);
            inviterWorld = world?.Name.ExtractText() ?? "";
        }
        catch (Exception ex)
        {
            RunLog.Debug("Party invite: failed to read the inviter: " + ex.Message);
        }
    }

    private string DisplayName()
    {
        if (!string.IsNullOrEmpty(inviterName))
        {
            if (!string.IsNullOrEmpty(inviterWorld))
            {
                return inviterName + "@" + inviterWorld;
            }
            return inviterName;
        }
        return "a player";
    }
}
