namespace AutoTreasureMaps.Core.Tasks;

// The stop conditions a run checks before each map. A map under way is always finished first.
internal static class RunLimits
{
    private const int KnockoutsBeforeStop = 2;

    public static RunEnd? Reached(TreasureSession session, Configuration configuration)
    {
        if (session.MapsKnockedOut >= KnockoutsBeforeStop)
        {
            return RunEnd.KnockedOutTwice;
        }

        if (session.FailedInARow >= Math.Max(1, configuration.RunFailureLimit))
        {
            return RunEnd.FailureStreak;
        }

        if (configuration.RunMapLimit > 0 && session.MapsRun >= configuration.RunMapLimit)
        {
            return RunEnd.MapLimit;
        }

        if (configuration.RunMinuteLimit > 0 && session.Elapsed.TotalMinutes >= configuration.RunMinuteLimit)
        {
            return RunEnd.TimeLimit;
        }

        return null;
    }
}
