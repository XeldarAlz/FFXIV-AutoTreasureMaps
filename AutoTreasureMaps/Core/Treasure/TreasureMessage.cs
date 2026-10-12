namespace AutoTreasureMaps.Core.Treasure;

// A copy of one log message's integer parameters, taken while the game's message is still valid. Bit n of
// IntegerMask is set when parameter n was an integer.
internal readonly record struct TreasureMessage(uint Id, long AtMs, int First, int Second, int Third, byte IntegerMask)
{
    public const int MaxParameters = 3;

    public bool TryGetParameter(int index, out int value)
    {
        value = index switch
        {
            0 => First,
            1 => Second,
            2 => Third,
            _ => 0,
        };
        return index is >= 0 and < MaxParameters && (IntegerMask & (1 << index)) != 0;
    }
}
