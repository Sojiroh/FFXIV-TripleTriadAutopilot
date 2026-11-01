using System;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Component.GUI;
using clib.Extensions;

namespace AutoTripleTriadGrind.Core.Game.Ops;

internal static class NpcInteraction
{
    private readonly record struct BlockingFlag(ConditionFlag Flag, string Label);

    public const int NoEntry = -1;

    private const int AddonInteractThrottleMs = 500;

    private const string SelectStringAddon = "SelectString";

    private const string SelectYesnoAddon = "SelectYesno";

    private const string TalkAddon = "Talk";

    private const string SelectStringThrottleKey = "TTA.Npc.SelectString";

    private const string SelectYesnoThrottleKey = "TTA.Npc.SelectYesno";

    private const string TalkThrottleKey = "TTA.Npc.Talk";

    private const string CallbackThrottleKey = "TTA.Npc.Callback";

    private const string CloseThrottleKey = "TTA.Npc.Close";

    private const float EyeHeightMeters = 1.7f;

    private const float MinSightLineMeters = 0.1f;

    private static readonly BlockingFlag[] blockingFlags = new BlockingFlag[6]
    {
        new BlockingFlag(ConditionFlag.Mounted, "mounted"),
        new BlockingFlag(ConditionFlag.MountOrOrnamentTransition, "mount transition"),
        new BlockingFlag(ConditionFlag.Jumping, "jumping"),
        new BlockingFlag(ConditionFlag.Jumping61, "airborne"),
        new BlockingFlag(ConditionFlag.InCombat, "in combat"),
        new BlockingFlag(ConditionFlag.Casting, "casting")
    };

    public static bool PlayerReady()
    {
        if (Svc.Objects.LocalPlayer == null)
        {
            return false;
        }
        for (int i = 0; i < blockingFlags.Length; i++)
        {
            if (Svc.Condition[blockingFlags[i].Flag])
            {
                return false;
            }
        }
        if (!ECommons.GameHelpers.Player.IsAnimationLocked && !GenericHelpers.IsOccupied())
        {
            return ECommons.GameHelpers.Player.Interactable;
        }
        return false;
    }

    public static string DescribeBlockers()
    {
        var blockers = new StringBuilder();
        for (int i = 0; i < blockingFlags.Length; i++)
        {
            if (Svc.Condition[blockingFlags[i].Flag])
            {
                AppendBlocker(blockers, blockingFlags[i].Label);
            }
        }
        if (ECommons.GameHelpers.Player.IsAnimationLocked)
        {
            AppendBlocker(blockers, "animation lock");
        }
        if (GenericHelpers.IsOccupied())
        {
            AppendBlocker(blockers, "occupied");
        }
        if (!ECommons.GameHelpers.Player.Interactable)
        {
            AppendBlocker(blockers, "player not interactable");
        }
        if (blockers.Length != 0)
        {
            return blockers.ToString();
        }
        return "no blocker detected";
    }

    public static IGameObject? FindNearest(uint baseId)
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return null;
        }
        var objects = Svc.Objects;
        IGameObject? nearest = null;
        var nearestDistanceSquared = float.MaxValue;
        for (int i = 0; i < objects.Length; i++)
        {
            var candidate = objects[i];
            if (candidate != null && candidate.BaseId == baseId && candidate.IsTargetable)
            {
                var distanceSquared = Vector3.DistanceSquared(localPlayer.Position, candidate.Position);
                if (!(distanceSquared >= nearestDistanceSquared))
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }
        }
        return nearest;
    }

    public static bool CanTalkTo(IGameObject npc)
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null || !npc.IsInInteractRange())
        {
            return false;
        }
        var playerPosition = localPlayer.Position;
        var npcPosition = npc.Position;
        var eye = new Vector3(playerPosition.X, playerPosition.Y + EyeHeightMeters, playerPosition.Z);
        var sightLine = new Vector3(npcPosition.X, npcPosition.Y + EyeHeightMeters, npcPosition.Z) - eye;
        var distance = sightLine.Length();
        RaycastHit hitInfo;
        if (!(distance <= MinSightLineMeters))
        {
            return !BGCollisionModule.RaycastMaterialFilter(eye, sightLine / distance, out hitInfo, distance);
        }
        return true;
    }

    public static bool IsTargeted(IGameObject gameObject)
    {
        return Svc.Targets.Target?.GameObjectId == gameObject.GameObjectId;
    }

    public static void Target(IGameObject gameObject)
    {
        Svc.Targets.Target = gameObject;
    }

    public unsafe static ulong Interact(IGameObject gameObject)
    {
        return TargetSystem.Instance()->InteractWithObject(gameObject.Struct(), checkLineOfSight: false);
    }

    public unsafe static bool IsAddonReady(string addonName)
    {
        AtkUnitBase* addon;
        return AddonReady(addonName, out addon);
    }

    public unsafe static bool SelectStringOpen()
    {
        AtkUnitBase* addon;
        return AddonReady(SelectStringAddon, out addon);
    }

    public unsafe static bool SelectYesnoOpen()
    {
        AtkUnitBase* addon;
        return AddonReady(SelectYesnoAddon, out addon);
    }

    public unsafe static bool TalkOpen()
    {
        AtkUnitBase* addon;
        return AddonReady(TalkAddon, out addon);
    }

    public static bool DialogAddonOpen()
    {
        if (!TalkOpen() && !SelectYesnoOpen())
        {
            return SelectStringOpen();
        }
        return true;
    }

    public unsafe static int SelectStringEntryCount()
    {
        if (!AddonReady(SelectStringAddon, out var addon))
        {
            return 0;
        }
        return new AddonMaster.SelectString(addon).EntryCount;
    }

    public unsafe static string SelectStringEntryText(int entryIndex)
    {
        if (!AddonReady(SelectStringAddon, out var addon))
        {
            return string.Empty;
        }
        var selectString = new AddonMaster.SelectString(addon);
        if (entryIndex < 0 || entryIndex >= selectString.EntryCount)
        {
            return string.Empty;
        }
        return selectString.Entries[entryIndex].Text;
    }

    public unsafe static int FindSelectStringEntry(string text)
    {
        if (text.Length == 0 || !AddonReady(SelectStringAddon, out var addon))
        {
            return NoEntry;
        }
        var entries = new AddonMaster.SelectString(addon).Entries;
        for (int i = 0; i < entries.Length; i++)
        {
            if (string.Equals(entries[i].Text.Trim(), text, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return NoEntry;
    }

    public unsafe static bool SelectEntry(int entryIndex)
    {
        if (!AddonReady(SelectStringAddon, out var addon) || entryIndex < 0 || entryIndex >= new AddonMaster.SelectString(addon).EntryCount)
        {
            return false;
        }
        if (!EzThrottler.Throttle(SelectStringThrottleKey))
        {
            return false;
        }
        RunLog.Debug($"SelectString: choosing entry {entryIndex}");
        Callback.Fire(addon, true, entryIndex);
        return true;
    }

    public static bool SelectLastEntry()
    {
        var entryCount = SelectStringEntryCount();
        if (entryCount > 0)
        {
            return SelectEntry(entryCount - 1);
        }
        return false;
    }

    public unsafe static string SelectYesnoText()
    {
        if (!AddonReady(SelectYesnoAddon, out var addon))
        {
            return string.Empty;
        }
        return new AddonMaster.SelectYesno(addon).Text;
    }

    public unsafe static bool AnswerNo()
    {
        if (!AddonReady(SelectYesnoAddon, out var addon) || !EzThrottler.Throttle(SelectYesnoThrottleKey))
        {
            return false;
        }
        new AddonMaster.SelectYesno(addon).No();
        return true;
    }

    public unsafe static bool ProgressTalk()
    {
        if (!AddonReady(TalkAddon, out var addon) || !EzThrottler.Throttle(TalkThrottleKey))
        {
            return false;
        }
        new AddonMaster.Talk(addon).Click();
        return true;
    }

    public unsafe static bool FireCallback(string addonName, int value)
    {
        if (!AddonReady(addonName, out var addon) || !EzThrottler.Throttle(CallbackThrottleKey))
        {
            return false;
        }
        RunLog.Debug($"{addonName}: firing callback {value}");
        Callback.Fire(addon, true, value);
        return true;
    }

    public unsafe static bool Close(string addonName)
    {
        if (!AddonReady(addonName, out var addon) || !EzThrottler.Throttle(CloseThrottleKey))
        {
            return false;
        }
        addon->Close(fireCallback: true);
        return true;
    }

    private static void AppendBlocker(StringBuilder builder, string label)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }
        builder.Append(label);
    }

    private unsafe static bool AddonReady(string addonName, out AtkUnitBase* addon)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out addon))
        {
            return GenericHelpers.IsAddonReady(addon);
        }
        return false;
    }
}
