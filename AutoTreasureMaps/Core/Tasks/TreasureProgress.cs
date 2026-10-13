using AutoTreasureMaps.Core.Treasure;

namespace AutoTreasureMaps.Core.Tasks;

// What the windows show of a run while it goes. The last map's result outlives the run, so the Treasure page can still
// show how the last map went once the run has ended.
internal sealed class TreasureProgress
{
    public TreasurePhase Phase { get; private set; } = TreasurePhase.Idle;

    public bool HasMap { get; private set; }

    public TreasureTier Tier { get; private set; }

    public DigSpot Spot { get; private set; }

    public int Wave { get; private set; }

    public int GuardsLeft { get; private set; }

    // Server unix seconds, 0 while the hunt's end has not been read.
    public long HuntEndsAt { get; private set; }

    public MapResult? LastMap { get; private set; }

    public void SetPhase(TreasurePhase phase) => Phase = phase;

    public void BeginMap(in TreasureTier tier, in DigSpot spot)
    {
        ClearMap();
        HasMap = true;
        Tier = tier;
        Spot = spot;
    }

    public void SetFight(int wave, int guardsLeft)
    {
        Wave = wave;
        GuardsLeft = guardsLeft;
    }

    public void SetHuntEnd(long endTimestamp) => HuntEndsAt = endTimestamp;

    public void FinishMap(in MapResult result)
    {
        LastMap = result;
        ClearMap();
    }

    public void Reset()
    {
        Phase = TreasurePhase.Idle;
        ClearMap();
    }

    private void ClearMap()
    {
        HasMap = false;
        Tier = default;
        Spot = default;
        Wave = 0;
        GuardsLeft = 0;
        HuntEndsAt = 0;
    }
}
