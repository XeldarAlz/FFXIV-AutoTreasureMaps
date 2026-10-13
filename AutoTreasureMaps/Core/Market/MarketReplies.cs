namespace AutoTreasureMaps.Core.Market;

internal enum PurchaseResult : byte
{
    Bought,
    Throttled,
    ListingGone,
    UniqueHeld,
    BagFull,
    NotEnoughGil,
    OwnRetainer,
}

// The game's answer to a purchase is a LogMessage row id, 0 for a purchase that went through.
internal static class MarketReplies
{
    public const int RequestRateLimited = 0x7000_0003;

    private const uint Success = 0;
    private const uint PurchaseWaitAndRetry = 387;
    private const uint PurchaseInventoryFull = 388;
    private const uint PurchaseUniqueHeld = 389;
    private const uint TransactionWaitAndRetry = 391;
    private const uint InsufficientGil = 392;
    private const uint OwnRetainerListing = 468;

    // The row the game sends when another buyer took the listing first is not known (386 and 390 are the likely ones),
    // so every other refusal reads as a listing that went or changed.
    public static PurchaseResult Classify(uint messageId) => messageId switch
    {
        Success => PurchaseResult.Bought,
        PurchaseWaitAndRetry or TransactionWaitAndRetry => PurchaseResult.Throttled,
        PurchaseInventoryFull => PurchaseResult.BagFull,
        PurchaseUniqueHeld => PurchaseResult.UniqueHeld,
        InsufficientGil => PurchaseResult.NotEnoughGil,
        OwnRetainerListing => PurchaseResult.OwnRetainer,
        _ => PurchaseResult.ListingGone,
    };
}
