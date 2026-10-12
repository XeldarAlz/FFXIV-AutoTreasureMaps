namespace AutoTreasureMaps.Core.Tasks;

// What the windows show of a run while it goes.
internal sealed class TreasureProgress
{
    public TreasurePhase Phase { get; private set; } = TreasurePhase.Idle;

    public void SetPhase(TreasurePhase phase) => Phase = phase;

    public void Reset() => Phase = TreasurePhase.Idle;
}
