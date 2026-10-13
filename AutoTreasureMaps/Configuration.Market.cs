using Newtonsoft.Json;

namespace AutoTreasureMaps;

public sealed partial class Configuration
{
    public bool MarketBuyingEnabled { get; set; } = false;

    // Gil per map before the market tax, by map rank. A tier missing here is never bought.
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<byte, int> MarketPriceCaps { get; set; } = [];

    // Market tax included.
    public int MarketRunBudget { get; set; } = 100_000;

    public int MarketKeepGil { get; set; } = 100_000;

    public MarketBoardChoice MarketBoard { get; set; } = MarketBoardChoice.Automatic;
}
