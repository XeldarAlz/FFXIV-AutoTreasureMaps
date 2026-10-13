using AutoTreasureMaps.Core.External;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Numerics;

namespace AutoTreasureMaps.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, NotBuilt, Running, Paused }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft, FontAwesomeIcon Icon, string Title, string Detail);

    private static int cachedFrame = -1;
    private static Info cached;

    public static Info Resolve(TreasureController controller)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedFrame)
        {
            return cached;
        }

        cached = Compute(controller);
        cachedFrame = frame;
        return cached;
    }

    private static Info Compute(TreasureController controller)
    {
        if (controller.Running)
        {
            if (controller.Paused)
            {
                var detail = controller.PauseReason == PauseReason.InContent
                    ? Loc.T(L.Treasure.DetailPausedInContent)
                    : Loc.T(L.Treasure.DetailPausedManual);
                return new Info(Kind.Paused, Styling.AccentAmber, Styling.AccentAmberSoft, FontAwesomeIcon.Pause, Loc.T(L.Treasure.TitlePaused), detail);
            }

            return new Info(Kind.Running, Styling.AccentBlue, Styling.AccentBlueSoft, FontAwesomeIcon.Map, Loc.T(L.Treasure.TitleRunning), PhaseLabel(controller.Phase));
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, FontAwesomeIcon.ExclamationTriangle,
                Loc.T(L.Treasure.TitleSetupNeeded), Loc.T(L.Treasure.DetailSetupNeeded));
        }

        return new Info(Kind.NotBuilt, Styling.AccentAmber, Styling.AccentAmberSoft, FontAwesomeIcon.Hammer,
            Loc.T(L.Treasure.TitleNotBuilt), Loc.T(L.Treasure.DetailNotBuilt));
    }

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => Loc.T(L.Shell.StatusRunning),
        Kind.Paused      => Loc.T(L.Shell.StatusPaused),
        Kind.SetupNeeded => Loc.T(L.Shell.StatusSetupNeeded),
        _                => Loc.T(L.Shell.StatusIdle),
    };

    public static string PhaseLabel(TreasurePhase phase) => phase switch
    {
        TreasurePhase.Preparing   => Loc.T(L.Run.PhasePreparing),
        TreasurePhase.Deciphering => Loc.T(L.Run.PhaseDeciphering),
        TreasurePhase.Travelling  => Loc.T(L.Run.PhaseTravelling),
        TreasurePhase.Digging     => Loc.T(L.MapRun.PhaseDigging),
        TreasurePhase.Opening     => Loc.T(L.MapRun.PhaseOpening),
        TreasurePhase.Fighting    => Loc.T(L.Run.PhaseFighting),
        TreasurePhase.Looting     => Loc.T(L.MapRun.PhaseLooting),
        TreasurePhase.Upkeep      => Loc.T(L.Progress.PhaseUpkeep),
        TreasurePhase.Finishing   => Loc.T(L.Run.PhaseFinishing),
        _                         => Loc.T(L.Run.PhaseStandingBy),
    };
}
