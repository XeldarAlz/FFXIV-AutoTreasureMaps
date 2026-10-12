namespace AutoTreasureMaps.Core.Treasure;

internal enum HeldMapSource : byte
{
    Native,
    KeyItemSlot,
}

// SpotIndex is the subrow of the tier's TreasureSpot row the deciphered map points at.
internal readonly record struct HeldMap(byte Rank, ushort SpotIndex, HeldMapSource Source);
