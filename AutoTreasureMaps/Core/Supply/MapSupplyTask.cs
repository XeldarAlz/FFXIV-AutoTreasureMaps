using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using System.Threading.Tasks;

namespace AutoTreasureMaps.Core.Supply;

internal enum MapSupplyOutcome : byte
{
    Supplied,
    Empty,
    Failed,
    Cancelled,
}

// Empty means the step has nothing left to give, so the run asks the next one; Failed means it tried and could not.
// Reason is set for both.
internal readonly record struct MapSupplyAnswer(MapSupplyOutcome Outcome, LocString Reason)
{
    public static MapSupplyAnswer Supplied => new(MapSupplyOutcome.Supplied, default);

    public static MapSupplyAnswer Cancelled => new(MapSupplyOutcome.Cancelled, default);

    public static MapSupplyAnswer Empty(LocString reason) => new(MapSupplyOutcome.Empty, reason);

    public static MapSupplyAnswer Failed(LocString reason) => new(MapSupplyOutcome.Failed, reason);
}

// A supply step's work, run as its own task so the run can stop it with the run and cap its time. The task runner
// swallows exceptions, so one that escapes the step is kept in Fault for the run to treat as a fault of its own.
internal abstract class MapSupplyTask : AutoCommon
{
    public MapSupplyAnswer Answer { get; private set; } = MapSupplyAnswer.Cancelled;

    public Exception? Fault { get; private set; }

    protected sealed override async Task Execute()
    {
        try
        {
            Answer = await Supply();
        }
        catch (OperationCanceledException)
        {
            Answer = MapSupplyAnswer.Cancelled;
        }
        catch (Exception exception)
        {
            Fault = exception;
            Answer = MapSupplyAnswer.Cancelled;
        }
        finally
        {
            // The run does not wait for a cancelled step to unwind, so a fight inside the step can take the movement
            // hold again after a Stop or a Pause has released it.
            if (CancelToken.IsCancellationRequested)
            {
                ReleaseCombatMovement("supply");
            }
        }
    }

    protected abstract Task<MapSupplyAnswer> Supply();
}
