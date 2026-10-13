namespace AutoTreasureMaps.Core.Localization;

internal static partial class L
{
    internal static class Humanizer
    {
        public static readonly LocString Group = new("humanizer.group", "Reaction delays");
        public static readonly LocString Enabled = new("humanizer.enabled", "Humanize reactions");
        public static readonly LocString EnabledHelp = new("humanizer.enabledHelp", "Wait a random moment before each action below, drawn anew each time between the shortest and the longest value. A shortest value above the longest is used as a fixed wait. Off acts at once.");
        public static readonly LocString RangeTo = new("humanizer.rangeTo", "to");
        public static readonly LocString SecondsFormat = new("humanizer.secondsFormat", "%.1f s");
        public static readonly LocString MoveOff = new("humanizer.moveOff", "Setting out for a map");
        public static readonly LocString MoveOffHelp = new("humanizer.moveOffHelp", "Once a map is in hand, before the run sets out for its dig spot.");
        public static readonly LocString Teleport = new("humanizer.teleport", "Casting a teleport");
        public static readonly LocString TeleportHelp = new("humanizer.teleportHelp", "Before each teleport the run casts: on the way to a dig spot, to a repair or to a city break.");
        public static readonly LocString Dig = new("humanizer.dig", "Digging");
        public static readonly LocString DigHelp = new("humanizer.digHelp", "Once at the dig spot, before each use of Dig.");
        public static readonly LocString OpenCoffer = new("humanizer.openCoffer", "Opening the coffer");
        public static readonly LocString OpenCofferHelp = new("humanizer.openCofferHelp", "Once beside the coffer the dig turned up, before opening it.");
        public static readonly LocString Engage = new("humanizer.engage", "Engaging the guards");
        public static readonly LocString EngageHelp = new("humanizer.engageHelp", "Once the coffer has opened and its guards appear, before joining the fight.");
    }
}
