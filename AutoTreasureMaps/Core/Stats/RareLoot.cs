namespace AutoTreasureMaps.Core.Stats;

// The loot worth counting on its own for each map, keyed by the timeworn map's item id. Only these items are tallied
// per map and priced into the gil a run made; everything else stays in the plain item count.
internal static class RareLoot
{
    // G17 Timeworn Br'aaxskin Map and G18 Timeworn Gargantuaskin Map.
    public const uint BraaxskinMap = 43557;
    public const uint GargantuaskinMap = 46185;

    private const uint DreamscapeWeaponCoffer = 45070; // Figmental Weapon Coffer
    private const uint LambHorn = 46829;               // Yan Horn
    private const uint WeeLamb = 47964;                // Mini Yan
    private const uint EnergyDrinkGg = 46773;          // DAM
    private const uint Grabbycat = 47965;              // Gimme Kitten
    private const uint AthleticCloth = 52630;          // Sweatcloth
    private const uint IndigoTwill = 52631;            // Indigo Twill Cloth

    private static readonly Dictionary<uint, uint[]> itemsByMap = new()
    {
        [GargantuaskinMap] = [DreamscapeWeaponCoffer, LambHorn, WeeLamb, EnergyDrinkGg, Grabbycat],
        [BraaxskinMap] = [DreamscapeWeaponCoffer, IndigoTwill, AthleticCloth],
    };

    private static readonly uint[] allItems = itemsByMap.Values.SelectMany(static items => items).Distinct().ToArray();

    // Every item listed for any map, for one batched price request.
    public static ReadOnlySpan<uint> AllItems => allItems;

    public static ReadOnlySpan<uint> For(uint mapItemId)
        => itemsByMap.TryGetValue(mapItemId, out var items) ? items : [];

    // Listed for any map; the loot cleanup never discards these.
    public static bool IsRare(uint itemId) => Array.IndexOf(allItems, itemId) >= 0;

    public static bool Counts(uint mapItemId, uint itemId)
        => itemsByMap.TryGetValue(mapItemId, out var items) && Array.IndexOf(items, itemId) >= 0;
}
