using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.Globalization;
using System.Numerics;

namespace AutoTreasureMaps.Core.Travel;

// The game's world to map projection, the one its map window and map links show.
internal static class MapCoordinates
{
    private const float MapTiles = 41f;
    private const float MapPixels = 2048f;
    private const float MapHalfPixels = 1024f;
    private const float PercentScale = 100f;
    private const float Tenths = 10f;

    public static bool TryToMap(uint mapId, Vector3 world, out Vector2 mapCoordinates)
    {
        mapCoordinates = default;
        if (Svc.Data.GetExcelSheet<Map>().GetRowOrDefault(mapId) is not { } map)
        {
            return false;
        }

        var scale = map.SizeFactor / PercentScale;
        if (scale <= 0f)
        {
            return false;
        }

        mapCoordinates = new Vector2(ToMap(world.X, scale, map.OffsetX), ToMap(world.Z, scale, map.OffsetY));
        return float.IsFinite(mapCoordinates.X) && float.IsFinite(mapCoordinates.Y);
    }

    // The map window cuts to one decimal instead of rounding, so 38.56 reads 38.5 there and has to read the same here.
    public static string Format(float mapCoordinate)
        => (MathF.Floor(mapCoordinate * Tenths) / Tenths).ToString("F1", CultureInfo.InvariantCulture);

    // map = 41 / scale * ((world + offset) * scale + 1024) / 2048 + 1.
    private static float ToMap(float worldCoordinate, float scale, int offset)
        => MapTiles / scale * (((worldCoordinate + offset) * scale + MapHalfPixels) / MapPixels) + 1f;
}
