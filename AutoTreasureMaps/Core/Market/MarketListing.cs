namespace AutoTreasureMaps.Core.Market;

// One row of the board's listings, copied out of the game's store. Total is what buying it charges.
internal readonly record struct MarketListing(ulong ListingId, ulong RetainerId, uint UnitPrice, uint TotalTax, uint Quantity)
{
    public long Total => (long)UnitPrice * Quantity + TotalTax;
}
