namespace AutoTreasureMaps.Core.Treasure;

internal enum HeldSpotState : byte
{
    None,
    Unresolved,
    Resolved,
}

internal static class HeldSpot
{
    public static HeldSpotState Read(TreasureCatalog catalog, out HeldMap held, out TreasureTier tier, out DigSpot spot)
    {
        tier = default;
        spot = default;
        if (!HeldMapReader.TryRead(catalog, out held))
        {
            return HeldSpotState.None;
        }

        return catalog.TryGetByRank(held.Rank, out tier) && DigSpotResolver.TryResolve(held.Rank, held.SpotIndex, out spot)
            ? HeldSpotState.Resolved
            : HeldSpotState.Unresolved;
    }
}
