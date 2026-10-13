namespace AutoTreasureMaps.Core.Supply;

// Where the run's maps come from, in the order it tries them. A new source is one entry here: a class implementing
// IMapSupplyStep, whose task derives from MapSupplyTask.
internal static class MapSupplyChain
{
    private static readonly IMapSupplyStep[] steps =
    [
        new BagSupplyStep(),
        new GatherSupplyStep(),
    ];

    public static IReadOnlyList<IMapSupplyStep> Steps => steps;

    // The counts of every step that can supply added up, or the first step's reason when none can.
    public static MapSupplyCheck Check(Plugin plugin)
    {
        var available = 0;
        var canSupply = false;
        MapSupplyCheck? firstRefusal = null;
        for (var index = 0; index < steps.Length; index++)
        {
            var check = steps[index].Check(plugin);
            if (check.CanSupply)
            {
                canSupply = true;
                available += check.Available;
                continue;
            }

            firstRefusal ??= check;
        }

        if (canSupply)
        {
            return MapSupplyCheck.Ready(available);
        }

        return firstRefusal ?? MapSupplyCheck.Unable(default);
    }
}
