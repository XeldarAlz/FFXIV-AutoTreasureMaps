namespace AutoTreasureMaps;

public sealed partial class Configuration
{
    // Off by default: the run then switches the character to a gatherer gearset and back, which a player should opt into.
    public bool GatherDailyMap { get; set; }

    public DailyMapGatherer DailyMapGatherer { get; set; } = DailyMapGatherer.Automatic;

    public int DailyMapNodeVisits { get; set; } = 8;
}
