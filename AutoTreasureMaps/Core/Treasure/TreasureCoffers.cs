using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using System.Numerics;
using NativeTreasure = FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure;

namespace AutoTreasureMaps.Core.Treasure;

// A dug-up coffer is a Treasure object of the treasure hunt kind. Another player's coffer at the same spot is the same
// kind, so a fresh dig prefers one that was not there before it. Read only on the framework thread.
internal static unsafe class TreasureCoffers
{
    public const int MaxRemembered = 8;

    public static IGameObject? FindNearest(Vector3 around, float withinMeters, ReadOnlySpan<ulong> skip)
    {
        var objects = Svc.Objects;
        IGameObject? nearest = null;
        var bestDistance = withinMeters * withinMeters;
        for (var index = 0; index < objects.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is null || !IsHuntCoffer(candidate) || skip.Contains(candidate.GameObjectId))
            {
                continue;
            }

            var distance = Vector3.DistanceSquared(around, candidate.Position);
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    public static int Collect(Vector3 around, float withinMeters, Span<ulong> into)
    {
        var objects = Svc.Objects;
        var limit = withinMeters * withinMeters;
        var count = 0;
        for (var index = 0; index < objects.Length && count < into.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is not null && IsHuntCoffer(candidate) && Vector3.DistanceSquared(around, candidate.Position) <= limit)
            {
                into[count++] = candidate.GameObjectId;
            }
        }

        return count;
    }

    public static bool IsHuntCoffer(IGameObject gameObject)
        => gameObject.ObjectKind == ObjectKind.Treasure
        && gameObject.Address != 0
        && ((NativeTreasure*)gameObject.Address)->CofferKind == NativeTreasure.TreasureKind.TreasureHunt;

    // A coffer that left the object table or started to fade has nothing more to give.
    public static bool Gone(ulong gameObjectId)
    {
        if (Svc.Objects.SearchById(gameObjectId) is not { } coffer || !IsHuntCoffer(coffer))
        {
            return true;
        }

        var treasure = (NativeTreasure*)coffer.Address;
        return treasure->State is NativeTreasure.TreasureState.FadingOut or NativeTreasure.TreasureState.FadedOut
            || (treasure->Flags & NativeTreasure.TreasureFlags.FadedOut) != 0;
    }

    public static string Describe(ulong gameObjectId)
    {
        if (Svc.Objects.SearchById(gameObjectId) is not { } coffer || !IsHuntCoffer(coffer))
        {
            return $"coffer {gameObjectId:X} (gone)";
        }

        var treasure = (NativeTreasure*)coffer.Address;
        return $"coffer {coffer.GameObjectId:X} (base {coffer.BaseId}, entity {coffer.EntityId:X}, state {treasure->State}, flags {treasure->Flags}, countdown {treasure->CountdownTime:F0}s, {treasure->ItemCount} items, SG row {treasure->ExportedSGRowId})";
    }
}
