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

    TreasurePhase Phase { get; }

    int TimeoutMs { get; }

    // False for a step that only puts a timeworn map in the bags; the run loops back and the bag step deciphers it.
    bool SuppliesHeldMap { get; }

    // Cheap enough for every frame: the Treasure page asks it whether a run can start.
    MapSupplyCheck Check(Plugin plugin);

    // Built afresh for every try, since a finished task cannot run again.
    MapSupplyTask CreateTask();
}
