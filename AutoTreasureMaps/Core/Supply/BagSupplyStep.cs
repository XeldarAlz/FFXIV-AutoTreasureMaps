using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Treasure;

namespace AutoTreasureMaps.Core.Supply;

// Deciphers the next enabled timeworn map in the bags, in the order the Maps settings choose.
internal sealed class BagSupplyStep : IMapSupplyStep
{
    // Two decipher attempts with every window's wait, and room to fight off an attacker and dismount first.
    private const int TaskTimeoutMs = 120_000;

    public string Name => "bags";

    public TreasurePhase Phase => TreasurePhase.Deciphering;

    public int TimeoutMs => TaskTimeoutMs;

    public bool SuppliesHeldMap => true;

    public MapSupplyCheck Check(Plugin plugin)
    {
        if (!DecipherOps.Unlocked())
        {
            return MapSupplyCheck.Unable(L.Decipher.NotUnlocked);
        }

        var issue = DecipherReadiness.PickNext(plugin.Catalog, plugin.Configuration, out _);
        return issue == DecipherIssue.None ? MapSupplyCheck.Ready(EnabledMaps(plugin)) : MapSupplyCheck.NothingToGive(DecipherIssues.Reason(issue));
    }

    public MapSupplyTask CreateTask() => new BagDecipherTask();

    private static int EnabledMaps(Plugin plugin)
    {
        var owned = MapBag.Read(plugin.Catalog);
        var total = 0;
        for (var index = 0; index < owned.Length; index++)
        {
            if (MapTierRules.IsEnabled(plugin.Configuration, owned[index].Tier))
            {
                total += owned[index].Count;
            }
        }

        return total;
    }
}
