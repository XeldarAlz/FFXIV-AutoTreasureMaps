using System.Numerics;

namespace AutoTreasureMaps.Core.Treasure;

internal readonly record struct DigSpot(
    string ZoneName,
    Vector3 Position,
    Vector2 MapCoordinates,
    uint LevelId,
    uint TerritoryId,
    uint MapId,
    ushort SpotIndex,
    byte Rank,
    bool FlyingUnlocked);
