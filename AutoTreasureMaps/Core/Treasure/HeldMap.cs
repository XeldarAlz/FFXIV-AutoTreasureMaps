namespace AutoTreasureMaps.Core.Treasure;

internal enum HeldMapSource : byte
{
    Native,
    KeyItemSlot,
}

internal readonly record struct HeldMap(byte Rank, ushort SpotIndex, HeldMapSource Source);
