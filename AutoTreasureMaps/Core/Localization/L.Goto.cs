namespace AutoTreasureMaps.Core.Localization;

internal static partial class L
{
    internal static class Goto
    {
        public static readonly LocString Busy = new("goto.busy", "Travel is already under way. /atm goto stop cancels it.");
        public static readonly LocString StopRunFirst = new("goto.stopRunFirst", "Stop the run before travelling.");
        public static readonly LocString NeedsPlugins = new("goto.needsPlugins", "Travel needs the required plugins. The Plugins page installs them.");
        public static readonly LocString HeadingToSpot = new("goto.headingToSpot", "Heading to the dig spot in {0}, X {1} Y {2}.");
        public static readonly LocString Arrived = new("goto.arrived", "Arrived in {0}.");
        public static readonly LocString LandFailed = new("goto.landFailed", "Arrived above the dig spot in {0}, but could not land. Land by hand before digging.");
        public static readonly LocString Failed = new("goto.failed", "Could not reach the destination in {0}. The console has the details.");
    }
}
