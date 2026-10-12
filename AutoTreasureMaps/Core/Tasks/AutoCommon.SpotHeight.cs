using AutoTreasureMaps.Core.Ipc;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoTreasureMaps.Core.Tasks;

public abstract partial class AutoCommon
{
    private const float SpotHeightSearchHalfExtentMeters = 5f;
    private const float SpotHeightFallbackHalfExtentMeters = 10f;
    private const float SpotHeightFallbackVerticalMeters = 300f;
    // Height hints are stored in 10 yalm steps, so the floor search starts one step above the hint.
    private const float SpotHeightHintLiftMeters = 10f;

    // A height hint picks the floor nearest it, so a spot under a bridge or below a ledge is not lifted to the top layer.
    private static Vector3? SnapSpotHeight(Vector3 point)
    {
        if (float.IsNaN(point.Y))
        {
            return SnapUnknownHeight(point);
        }

        var navmesh = NavmeshIPC.Instance;
        var lifted = point with { Y = point.Y + SpotHeightHintLiftMeters };
        return navmesh.PointOnFloor(lifted, allowUnlandable: false, SpotHeightSearchHalfExtentMeters)
            ?? navmesh.PointOnFloor(lifted, allowUnlandable: true, SpotHeightSearchHalfExtentMeters)
            ?? SnapUnknownHeight(point);
    }

    private static Vector3? SnapUnknownHeight(Vector3 point)
    {
        var navmesh = NavmeshIPC.Instance;
        var floor = navmesh.HighestFloor(point, allowUnlandable: false, SpotHeightSearchHalfExtentMeters)
            ?? navmesh.HighestFloor(point, allowUnlandable: true, SpotHeightSearchHalfExtentMeters);
        if (floor is not null)
        {
            return floor;
        }

        var height = Svc.Objects.LocalPlayer?.Position.Y ?? 0f;
        return navmesh.NearestStandablePoint(point with { Y = height }, SpotHeightFallbackHalfExtentMeters, SpotHeightFallbackVerticalMeters);
    }
}
