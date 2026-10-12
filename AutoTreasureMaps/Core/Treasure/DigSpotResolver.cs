using AutoTreasureMaps.Core.Travel;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoTreasureMaps.Core.Treasure;

// The chain the client itself walks to show a map: the rank's TreasureSpot subrow, its Level row, and that Level's
// territory and map. The sheet part is kept for the last spot asked about; flight access is read fresh on every call,
// since unlocking the zone's aether currents changes it.
internal static class DigSpotResolver
{
    private static bool cacheFilled;
    private static byte cachedRank;
    private static ushort cachedSpotIndex;
    private static bool cachedResolved;
    private static DigSpot cachedSpot;

    public static bool TryResolve(byte rank, ushort spotIndex, out DigSpot spot)
    {
        if (!cacheFilled || cachedRank != rank || cachedSpotIndex != spotIndex)
        {
            cachedResolved = TryResolveFromSheets(rank, spotIndex, out cachedSpot);
            cachedRank = rank;
            cachedSpotIndex = spotIndex;
            cacheFilled = true;
        }

        if (!cachedResolved)
        {
            spot = default;
            return false;
        }

        spot = cachedSpot with { FlyingUnlocked = FlightAccess.IsAvailableIn(cachedSpot.TerritoryId) };
        return true;
    }

    private static bool TryResolveFromSheets(byte rank, ushort spotIndex, out DigSpot spot)
    {
        spot = default;
        if (Svc.Data.GetSubrowExcelSheet<TreasureSpot>().GetSubrowOrDefault(rank, spotIndex) is not { } treasureSpot)
        {
            return false;
        }

        if (treasureSpot.Location.RowId == 0 || treasureSpot.Location.ValueNullable is not { } level)
        {
            return false;
        }

        var territoryId = level.Territory.RowId;
        var mapId = level.Map.RowId;
        var position = new Vector3(level.X, level.Y, level.Z);
        if (territoryId == 0 || !MapCoordinates.TryToMap(mapId, position, out var mapCoordinates))
        {
            return false;
        }

        spot = new DigSpot(TerritoryNames.Of(territoryId), position, mapCoordinates, level.RowId, territoryId, mapId, spotIndex, rank, FlyingUnlocked: false);
        return true;
    }
}
