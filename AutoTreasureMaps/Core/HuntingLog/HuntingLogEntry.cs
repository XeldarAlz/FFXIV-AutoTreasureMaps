namespace AutoTreasureMaps.Core.HuntingLog;

public readonly record struct HuntingLogEntry(
    byte Slot,
    byte Rank,
    byte EntryIndex,
    ushort FirstTarget,
    byte TargetCount);
