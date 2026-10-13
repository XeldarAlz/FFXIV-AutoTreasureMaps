using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;

namespace AutoTreasureMaps.Core.Supply;

// Whether a supply step can put a map in hand now. Available is how many maps it could hand the run as far as it can
// tell; Reason says why not when it cannot.
internal readonly record struct MapSupplyCheck(bool CanSupply, int Available, LocString Reason)
{
    public static MapSupplyCheck Ready(int available) => new(true, available, default);

    public static MapSupplyCheck Unable(LocString reason) => new(false, 0, reason);
}

// One way the run gets its next map into hand. Whenever no map is held, the run asks the steps of MapSupplyChain in
// order and runs the first that can supply; once every step says it cannot, the run is out of maps.
internal interface IMapSupplyStep
{
    // English, for the log.
    string Name { get; }

    // What the run shows while the step works.
    TreasurePhase Phase { get; }

    // How long the step's task may take before the run cancels it.
    int TimeoutMs { get; }

    // Cheap enough for every frame: the Treasure page asks it whether a run can start.
    MapSupplyCheck Check(Plugin plugin);

    // The step's work, built afresh for every try and run by the run task under the step's time limit.
    MapSupplyTask CreateTask();
}
