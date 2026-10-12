using System.Numerics;

namespace AutoTreasureMaps.Core.Treasure;

// Position is the spot's Level point, a full 3D point on the ground; MapCoordinates is what the game's map shows for it.
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
