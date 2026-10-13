using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Gather;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;

namespace AutoTreasureMaps.Core.Supply;

// Gathers the day's free timeworn map as a Miner or Botanist and deciphers it.
internal sealed class GatherSupplyStep : IMapSupplyStep
{
    // The trip, every node an attempt may open with the way between them, the gearset switches and the decipher after.
    private const int TaskTimeoutMs = 900_000;
    private const int OneMap = 1;

    public string Name => "gather";

    public TreasurePhase Phase => TreasurePhase.Gathering;

    public int TimeoutMs => TaskTimeoutMs;

    public bool SuppliesHeldMap => true;

    // The run deciphers the gathered map at once, so without Decipher unlocked the trip would be wasted.
    public MapSupplyCheck Check(Plugin plugin)
    {
        var refusal = DailyMapPlanner.CheckForRun(plugin, out _, out var reading);
        if (refusal != DailyMapRefusal.None)
        {
            return MapSupplyCheck.Unable(DailyMapPlanner.Reason(refusal, reading));
        }

        return DecipherOps.Unlocked() ? MapSupplyCheck.Ready(OneMap) : MapSupplyCheck.Unable(L.Decipher.NotUnlocked);
    }

    public MapSupplyTask CreateTask() => new GatherMapTask(forRun: true);
}
