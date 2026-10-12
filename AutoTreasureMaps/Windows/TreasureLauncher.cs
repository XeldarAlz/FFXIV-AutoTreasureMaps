using AutoTreasureMaps.Core.External;
using AutoTreasureMaps.Core.Localization;

namespace AutoTreasureMaps.Windows;

// What the Start button says. No treasure run exists yet, so Start only reaches the controller, which refuses it.
internal static class TreasureLauncher
{
    public enum Readiness : byte
    {
        SetupNeeded,
        NotBuilt,
    }

    public static Readiness Assess() => ExternalPlugins.AllRequiredInstalled() ? Readiness.NotBuilt : Readiness.SetupNeeded;

    public static void Start() => Plugin.Instance.Controller.Start();

    public static string Sublabel() => Loc.T(L.Treasure.StartSub);

    // Shown only while Start is disabled, which only missing plugins do.
    public static string Reason() => Loc.T(L.Treasure.ReasonInstall);
}
