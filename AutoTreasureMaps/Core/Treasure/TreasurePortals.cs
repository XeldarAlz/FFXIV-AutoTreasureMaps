using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoTreasureMaps.Core.Treasure;

// A coffer of a tier with a dungeon can leave a teleportation portal behind. The game data names four event objects
// "teleportation portal" and which one the overworld hunt spawns is not known yet, so any of them near the coffer counts.
// The run only notes a portal; it never interacts with one.
internal static class TreasurePortals
{
    private static readonly uint[] PortalBaseIds = [2007181, 2009036, 2009039, 2013488];

    public static IGameObject? FindNear(Vector3 around, float withinMeters)
    {
        var objects = Svc.Objects;
        var limit = withinMeters * withinMeters;
        for (var index = 0; index < objects.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is null || candidate.ObjectKind != ObjectKind.EventObj || Array.IndexOf(PortalBaseIds, candidate.BaseId) < 0)
            {
                continue;
            }

            if (Vector3.DistanceSquared(around, candidate.Position) <= limit)
            {
                return candidate;
            }
        }

        return null;
    }
}
