using System.Collections.Immutable;
using System.Numerics;

namespace AutoTreasureMaps.Core.Gather;

internal enum Gatherer : byte
{
    Miner,
    Botanist,
}

// One gathering area: BaseId is its GatheringPointBase row, PointIds the GatheringPoint rows a live node of it carries
// as its object BaseId, and Nodes the known spots those points spawn on.
internal readonly record struct MapGatherArea(
    ushort BaseId,
    ushort TerritoryId,
    ushort AetheryteId,
    Vector3 Centre,
    ushort Radius,
    ImmutableArray<uint> PointIds,
    ImmutableArray<Vector3> Nodes);

// The maps of one gathering level share their nodes, and the game picks which of them a node shows.
internal readonly record struct MapGatherGroup(
    byte Level,
    ImmutableArray<uint> MapItemIds,
    MapGatherArea Miner,
    MapGatherArea Botanist)
{
    public MapGatherArea AreaFor(Gatherer gatherer) => gatherer == Gatherer.Miner ? Miner : Botanist;
}

// The game data lists no node that yields a timeworn map: the server adds the map to an ordinary node of the right
// level, so the areas are a hand-picked table.
internal static partial class MapGatherTable
{
    private const uint MinerClassJobId = 16;
    private const uint BotanistClassJobId = 17;

    public static ReadOnlySpan<MapGatherGroup> Groups => groups;

    public static uint ClassJobId(Gatherer gatherer) => gatherer == Gatherer.Miner ? MinerClassJobId : BotanistClassJobId;

    public static bool IsGathererClassJob(uint classJobId) => classJobId is MinerClassJobId or BotanistClassJobId;

    public static bool Contains(ImmutableArray<uint> ids, uint id)
    {
        var span = ids.AsSpan();
        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] == id)
            {
                return true;
            }
        }

        return false;
    }
}
