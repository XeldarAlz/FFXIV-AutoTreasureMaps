using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Treasure;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace AutoTreasureMaps.Core.Game.Watchers;

internal sealed class DutyWatcher : IDisposable
{
    private const int ResumeSettleMs = 5_000;

    private long leftContentAtMs;
    private HuntDuty loggedHuntDuty;

    private enum HuntDuty : byte { None, MapStep, Director }

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
            leftContentAtMs = 0;
            if (Plugin.Instance.Configuration.AutoPauseInContent && controller.SessionSnapshot is not null && !HuntHoldsTheDuty(controller)) controller.Pause(PauseReason.InContent);
            return;
        }

        loggedHuntDuty = HuntDuty.None;
        if (controller.PauseReason != PauseReason.InContent) return;

        if (Svc.Objects.LocalPlayer is null
         || Svc.Condition[ConditionFlag.BetweenAreas]
         || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            leftContentAtMs = 0;
            return;
        }

        if (leftContentAtMs == 0)
        {
            leftContentAtMs = Environment.TickCount64;
            return;
        }
        if (Environment.TickCount64 - leftContentAtMs < ResumeSettleMs) return;

        leftContentAtMs = 0;
        controller.Resume();
    }

    // The game may count a dug-up coffer's hunt as a duty. That is the run's own work, not content to wait out, so the
    // pause is skipped while the map run is at the coffer or the hunt's director runs. Which of the two held it is
    // logged once per stretch, so a test run shows whether the flag is set at all.
    private bool HuntHoldsTheDuty(TreasureController controller)
    {
        var reason = controller.Phase is TreasurePhase.Digging or TreasurePhase.Opening or TreasurePhase.Fighting or TreasurePhase.Looting
            ? HuntDuty.MapStep
            : TreasureHuntDirector.IsActive() ? HuntDuty.Director : HuntDuty.None;
        if (reason != HuntDuty.None && reason != loggedHuntDuty)
        {
            RunLog.Info(reason == HuntDuty.MapStep
                ? $"Duty: bound by duty during the map run's {controller.Phase} step; not pausing."
                : "Duty: bound by duty while the treasure hunt director runs; not pausing.");
        }

        loggedHuntDuty = reason;
        return reason != HuntDuty.None;
    }

    internal static bool InContent()
        => Svc.Condition[ConditionFlag.BoundByDuty]
        || Svc.Condition[ConditionFlag.BoundByDuty56]
        || Svc.Condition[ConditionFlag.BoundByDuty95];
}
