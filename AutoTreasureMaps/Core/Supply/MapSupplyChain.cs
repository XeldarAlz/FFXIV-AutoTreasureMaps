namespace AutoTreasureMaps.Core.Supply;

// Where the run's maps come from, in the order it tries them. A new source is one entry here: a class implementing
// IMapSupplyStep, whose task derives from MapSupplyTask.
internal static class MapSupplyChain
{
    private static readonly IMapSupplyStep[] steps =
    [
        new BagSupplyStep(),
        new GatherSupplyStep(),
        new MarketSupplyStep(),
    ];

    public static IReadOnlyList<IMapSupplyStep> Steps => steps;

    // The counts of every step that can supply added up. When none can, a step held back speaks before one that is only
    // out of maps or switched off, so empty bags do not hide why a source the player turned on gives nothing.
    public static MapSupplyCheck Check(Plugin plugin)
    {
        var available = 0;
        var canSupply = false;
        MapSupplyCheck? firstRefusal = null;
        MapSupplyCheck? firstBlocked = null;
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
            if (check.Blocked)
            {
                firstBlocked ??= check;
            }
        }

        if (canSupply)
        {
            return MapSupplyCheck.Ready(available);
        }

        return firstBlocked ?? firstRefusal ?? MapSupplyCheck.Unable(default);
    }
}
