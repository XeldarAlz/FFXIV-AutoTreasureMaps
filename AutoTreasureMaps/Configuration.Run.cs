namespace AutoTreasureMaps;

public sealed partial class Configuration
{
    // 0 runs until the maps run out.
    public int RunMapLimit { get; set; }

    // Run time without pauses; 0 sets no limit.
    public int RunMinuteLimit { get; set; }

    // Maps in a row that end with nothing dug up, out of reach or on an error.
    public int RunFailureLimit { get; set; } = 3;
}
