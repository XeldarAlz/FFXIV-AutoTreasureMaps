using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Treasure;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AutoTreasureMaps.Core.Gather;

internal enum DailyMapRefusal : byte
{
    None,
    Off,
    AllowanceUnknown,
    AllowanceNotYet,
    NoGearset,
    NoMinerGearset,
    NoBotanistGearset,
    NoEnabledTier,
    LevelTooLow,
    AlreadyHeld,
    NoReturnGearset,
}

internal readonly record struct DailyMapPlan(Gatherer Gatherer, int GearsetIndex, int ClassLevel, int GroupIndex)
{
    public MapGatherGroup Group => MapGatherTable.Groups[GroupIndex];

    public MapGatherArea Area => Group.AreaFor(Gatherer);

    public uint ClassJobId => MapGatherTable.ClassJobId(Gatherer);
}

// Which gatherer gathers the daily map and which tier it goes for: the highest enabled tier its level allows, skipping
// tiers already in the bags. The run's check is cached for the frame, as the Treasure page asks it every frame.
internal static unsafe class DailyMapPlanner
{
    private const long NoFrame = -1;
    private const int MinutesPerHour = 60;
    // Not a catalog key: the text is already formatted in the current language.
    private const string FormattedWaitKey = "gather.reason.notYet.formatted";

    private static long cachedFrame = NoFrame;
    private static DailyMapRefusal cachedRefusal;
    private static DailyMapPlan cachedPlan;
    private static AllowanceReading cachedReading;

    private static long waitReasonMinutes = -1;
    private static LanguageInfo? waitReasonLanguage;
    private static LocString waitReason;

    public static DailyMapRefusal CheckForRun(Plugin plugin, out DailyMapPlan plan, out AllowanceReading reading)
    {
        var frame = CurrentFrame();
        if (frame == NoFrame || frame != cachedFrame)
        {
            cachedRefusal = Plan(plugin, honourSwitch: true, honourAllowance: true, out cachedPlan, out cachedReading);
            cachedFrame = frame;
        }

        plan = cachedPlan;
        reading = cachedReading;
        return cachedRefusal;
    }

    // A gather asked for by hand ignores the switch, and the task reads the allowance afresh itself.
    public static DailyMapRefusal Plan(Plugin plugin, bool honourSwitch, bool honourAllowance, out DailyMapPlan plan, out AllowanceReading reading)
    {
        plan = default;
        reading = MapAllowance.Read();
        var configuration = plugin.Configuration;
        if (honourSwitch && !configuration.GatherDailyMap)
        {
            return DailyMapRefusal.Off;
        }

        if (honourAllowance && reading.State != AllowanceState.Available)
        {
            return reading.State == AllowanceState.Waiting ? DailyMapRefusal.AllowanceNotYet : DailyMapRefusal.AllowanceUnknown;
        }

        var refusal = PickGatherer(configuration.DailyMapGatherer, out var gatherer, out var gearsetIndex, out var level);
        if (refusal != DailyMapRefusal.None)
        {
            return refusal;
        }

        refusal = PickGroup(plugin, gatherer, gearsetIndex, level, out plan);
        if (refusal != DailyMapRefusal.None || CanComeBack(plan.ClassJobId))
        {
            return refusal;
        }

        plan = default;
        return DailyMapRefusal.NoReturnGearset;
    }

    // A map of the group the run can use: its tier is enabled and none of it is in the bags yet.
    public static bool IsWanted(Plugin plugin, uint mapItemId)
    {
        var catalog = plugin.Catalog;
        if (!catalog.TryGetByTimewornItem(mapItemId, out var tier) || !MapTierRules.IsEnabled(plugin.Configuration, tier))
        {
            return false;
        }

        return !MapBag.TryFind(catalog, tier.Rank, out _);
    }

    public static LocString Reason(DailyMapRefusal refusal, in AllowanceReading reading) => refusal switch
    {
        DailyMapRefusal.Off => L.Gather.ReasonOff,
        DailyMapRefusal.AllowanceUnknown => L.Gather.ReasonUnknown,
        DailyMapRefusal.AllowanceNotYet => WaitReason(reading.SecondsLeft),
        DailyMapRefusal.NoGearset => L.Gather.ReasonNoGearset,
        DailyMapRefusal.NoMinerGearset => L.Gather.ReasonNoMinerGearset,
        DailyMapRefusal.NoBotanistGearset => L.Gather.ReasonNoBotanistGearset,
        DailyMapRefusal.NoEnabledTier => L.Gather.ReasonNoEnabledTier,
        DailyMapRefusal.LevelTooLow => L.Gather.ReasonLevelTooLow,
        DailyMapRefusal.AlreadyHeld => L.Gather.ReasonAlreadyHeld,
        DailyMapRefusal.NoReturnGearset => L.Gather.ReasonNoReturnGearset,
        _ => default,
    };

    public static string Duration(long secondsLeft)
    {
        var minutes = Math.Max(1, (secondsLeft + TimeUnits.SecondsPerMinute - 1) / TimeUnits.SecondsPerMinute);
        var hours = minutes / MinutesPerHour;
        return hours > 0
            ? Loc.T(L.Gather.HoursMinutes, hours, minutes % MinutesPerHour)
            : Loc.T(L.Gather.Minutes, minutes);
    }

    public static LocString GathererName(Gatherer gatherer)
        => gatherer == Gatherer.Miner ? L.Gather.GathererMinerName : L.Gather.GathererBotanistName;

    // The switch back looks for a gearset of the class worn now, or of the one an earlier gather still owes; without one
    // the character would be left on the gatherer.
    private static bool CanComeBack(uint gathererClassJobId)
    {
        if (GatherGearsetReturn.TryGetOwed(out var owed))
        {
            return GearsetSwitcher.HasGearsetFor(owed.ClassJobId, owed.GearsetIndex);
        }

        var classJobId = GearsetSwitcher.CurrentClassJobId();
        return classJobId == gathererClassJobId || GearsetSwitcher.HasGearsetFor(classJobId, GearsetSwitcher.CurrentGearsetIndex());
    }

    private static DailyMapRefusal PickGatherer(DailyMapGatherer preference, out Gatherer gatherer, out int gearsetIndex, out int level)
    {
        var minerGearset = GearsetSwitcher.FindBestGearset(MapGatherTable.ClassJobId(Gatherer.Miner));
        var botanistGearset = GearsetSwitcher.FindBestGearset(MapGatherTable.ClassJobId(Gatherer.Botanist));
        var hasMiner = minerGearset != GearsetSwitcher.NoGearset;
        var hasBotanist = botanistGearset != GearsetSwitcher.NoGearset;
        gatherer = Gatherer.Miner;
        gearsetIndex = GearsetSwitcher.NoGearset;
        level = 0;
        switch (preference)
        {
            case DailyMapGatherer.Miner when !hasMiner:
                return DailyMapRefusal.NoMinerGearset;
            case DailyMapGatherer.Botanist when !hasBotanist:
                return DailyMapRefusal.NoBotanistGearset;
            case DailyMapGatherer.Automatic when !hasMiner && !hasBotanist:
                return DailyMapRefusal.NoGearset;
            case DailyMapGatherer.Miner:
                gatherer = Gatherer.Miner;
                break;
            case DailyMapGatherer.Botanist:
                gatherer = Gatherer.Botanist;
                break;
            default:
                var minerLevel = hasMiner ? GearsetSwitcher.ClassLevel(MapGatherTable.ClassJobId(Gatherer.Miner)) : -1;
                var botanistLevel = hasBotanist ? GearsetSwitcher.ClassLevel(MapGatherTable.ClassJobId(Gatherer.Botanist)) : -1;
                gatherer = botanistLevel > minerLevel ? Gatherer.Botanist : Gatherer.Miner;
                break;
        }

        gearsetIndex = gatherer == Gatherer.Miner ? minerGearset : botanistGearset;
        level = GearsetSwitcher.ClassLevel(MapGatherTable.ClassJobId(gatherer));
        return DailyMapRefusal.None;
    }

    private static DailyMapRefusal PickGroup(Plugin plugin, Gatherer gatherer, int gearsetIndex, int level, out DailyMapPlan plan)
    {
        var groups = MapGatherTable.Groups;
        var anyEnabled = false;
        var anyWithinLevel = false;
        for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            var group = groups[groupIndex];
            if (!AnyEnabled(plugin, group))
            {
                continue;
            }

            anyEnabled = true;
            if (group.Level > level)
            {
                continue;
            }

            anyWithinLevel = true;
            if (!AnyWanted(plugin, group))
            {
                continue;
            }

            plan = new DailyMapPlan(gatherer, gearsetIndex, level, groupIndex);
            return DailyMapRefusal.None;
        }

        plan = default;
        if (!anyEnabled)
        {
            return DailyMapRefusal.NoEnabledTier;
        }

        return anyWithinLevel ? DailyMapRefusal.AlreadyHeld : DailyMapRefusal.LevelTooLow;
    }

    private static bool AnyEnabled(Plugin plugin, in MapGatherGroup group)
    {
        var mapItemIds = group.MapItemIds.AsSpan();
        for (var index = 0; index < mapItemIds.Length; index++)
        {
            if (plugin.Catalog.TryGetByTimewornItem(mapItemIds[index], out var tier) && MapTierRules.IsEnabled(plugin.Configuration, tier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AnyWanted(Plugin plugin, in MapGatherGroup group)
    {
        var mapItemIds = group.MapItemIds.AsSpan();
        for (var index = 0; index < mapItemIds.Length; index++)
        {
            if (IsWanted(plugin, mapItemIds[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static LocString WaitReason(long secondsLeft)
    {
        var minutes = secondsLeft / TimeUnits.SecondsPerMinute;
        if (minutes != waitReasonMinutes || !ReferenceEquals(waitReasonLanguage, Loc.Current))
        {
            waitReasonMinutes = minutes;
            waitReasonLanguage = Loc.Current;
            waitReason = new LocString(FormattedWaitKey, Loc.T(L.Gather.ReasonNotYet, Duration(secondsLeft)));
        }

        return waitReason;
    }

    private static long CurrentFrame()
    {
        var framework = Framework.Instance();
        return framework is null ? NoFrame : framework->FrameCounter;
    }
}
