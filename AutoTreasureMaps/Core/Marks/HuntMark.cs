using AutoTreasureMaps.Core.Hunts;

namespace AutoTreasureMaps.Core.Marks;

public readonly record struct HuntMark(
    uint NameId,
    HuntMarkRank Rank,
    ushort TerritoryId,
    ExpansionKind Expansion);
