namespace AutoTreasureMaps.Core.Treasure;

internal enum MapTierState : byte
{
    Enabled,
    Disabled,
    Locked,
}

internal static class MapTierRules
{
    // Undersea spots need diving, which the travel stack cannot do yet, so those tiers are locked off.
    public static MapTierState StateOf(Configuration configuration, in TreasureTier tier)
    {
        if (tier.Undersea)
        {
            return MapTierState.Locked;
        }

        return configuration.DisabledMapRanks.Contains(tier.Rank) ? MapTierState.Disabled : MapTierState.Enabled;
    }

    public static bool IsEnabled(Configuration configuration, in TreasureTier tier)
        => StateOf(configuration, tier) == MapTierState.Enabled;

    public static void SetEnabled(Configuration configuration, in TreasureTier tier, bool enabled)
    {
        if (tier.Undersea)
        {
            return;
        }

        var changed = enabled ? configuration.DisabledMapRanks.Remove(tier.Rank) : configuration.DisabledMapRanks.Add(tier.Rank);
        if (changed)
        {
            configuration.SaveDebounced();
        }
    }

    // Owned maps come in rank order, so the order setting only decides which end the walk starts from.
    public static bool TryPickNext(ReadOnlySpan<OwnedMap> owned, Configuration configuration, out OwnedMap pick)
    {
        var highestFirst = configuration.MapOrder == MapOrder.HighestFirst;
        for (var step = 0; step < owned.Length; step++)
        {
            var candidate = owned[highestFirst ? owned.Length - 1 - step : step];
            if (IsEnabled(configuration, candidate.Tier))
            {
                pick = candidate;
                return true;
            }
        }

        pick = default;
        return false;
    }
}
