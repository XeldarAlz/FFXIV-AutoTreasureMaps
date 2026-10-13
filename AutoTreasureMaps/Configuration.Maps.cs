using Newtonsoft.Json;

namespace AutoTreasureMaps;

public sealed partial class Configuration
{
    // Exclusions rather than a selection, so a tier a game update adds starts enabled. Undersea tiers stay off whatever
    // this holds.
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<byte> DisabledMapRanks { get; set; } = [];

    public MapOrder MapOrder { get; set; } = MapOrder.HighestFirst;

    // How the run answers the game's Need or Greed window for the coffer's rolled items.
    public LootRoll LootRoll { get; set; } = LootRoll.Need;
}
