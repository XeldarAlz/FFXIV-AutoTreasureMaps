using AutoTreasureMaps.Core.Localization;

namespace AutoTreasureMaps.Core.Changelog;

internal readonly record struct ChangelogEntry(string Version, string Date, LocString[] Highlights);
