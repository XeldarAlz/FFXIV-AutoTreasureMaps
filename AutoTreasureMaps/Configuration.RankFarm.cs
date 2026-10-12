using AutoTreasureMaps.Core.Hunts;

namespace AutoTreasureMaps;

public sealed partial class Configuration
{
    public bool FarmARanks { get; set; } = true;
    public bool FarmSRanks { get; set; }
    public HashSet<ExpansionKind> FarmExpansions { get; set; } = [];
    public bool FarmVisitWorlds { get; set; }
}
