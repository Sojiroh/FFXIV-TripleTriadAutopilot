using System;
using AutoTripleTriadGrind.Core.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace AutoTripleTriadGrind.Core.Game.Watchers;

internal sealed class DutyWatcher : IDisposable
{
    private const int ResumeSettleMs = 5000;

    private long leftContentAtMs;

    public DutyWatcher()
    {
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var controller = Plugin.Instance.Controller;
        if (InContent())
        {
            leftContentAtMs = 0L;
            if (Plugin.Instance.Configuration.AutoPauseInContent)
            {
                controller.Pause(PauseReason.InContent);
            }
            return;
        }
        if (controller.PauseReason != PauseReason.InContent)
        {
            return;
        }
        if (Svc.Objects.LocalPlayer == null || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            leftContentAtMs = 0L;
            return;
        }
        if (leftContentAtMs == 0L)
        {
            leftContentAtMs = Environment.TickCount64;
            return;
        }
        if (Environment.TickCount64 - leftContentAtMs < ResumeSettleMs)
        {
            return;
        }
        leftContentAtMs = 0L;
        controller.Resume();
    }

    private static bool InContent()
    {
        if (Svc.ClientState.TerritoryType != 579)
        {
            if (!Svc.Condition[ConditionFlag.BoundByDuty] && !Svc.Condition[ConditionFlag.BoundByDuty56])
            {
                return Svc.Condition[ConditionFlag.BoundByDuty95];
            }
            return true;
        }
        return false;
    }
}
