using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using System.Numerics;
using NativeObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace AutoTreasureMaps.Core.Treasure;

// Ours counts the live guards the game marks as this party's; Others counts the hunt's guards that carry neither mark,
// another party's, kept apart only for the log. Nearest is the targetable guard of ours closest to the character.
internal readonly record struct GuardSurvey(int Ours, int Others, IBattleNpc? Nearest);

// A coffer's guards are combatant battle NPCs run by the treasure hunt director. The game gives the ones that belong to
// the coffer's own party one of two nameplate icons, which count only on the director's guards, so no other enemy
// nearby is taken for one of ours. Read only on the framework thread.
internal static unsafe class TreasureGuards
{
    private const uint OwnGuardIcon = 60094;
    private const uint OwnGuardIconAlternate = 60096;

    public static GuardSurvey Survey(Vector3 around, float withinMeters)
    {
        var objects = Svc.Objects;
        var from = Svc.Objects.LocalPlayer?.Position ?? around;
        var limit = withinMeters * withinMeters;
        var ours = 0;
        var others = 0;
        IBattleNpc? nearest = null;
        var nearestDistance = float.MaxValue;
        for (var index = 0; index < objects.Length; index++)
        {
            if (objects[index] is not IBattleNpc npc || !IsLiveCombatant(npc) || Vector3.DistanceSquared(around, npc.Position) > limit)
            {
                continue;
            }

            if (!IsOurs(npc))
            {
                if (IsHuntGuard(npc))
                {
                    others++;
                }

                continue;
            }

            ours++;
            var distance = Vector3.DistanceSquared(from, npc.Position);
            if (npc.IsTargetable && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = npc;
            }
        }

        return new GuardSurvey(ours, others, nearest);
    }

    public static bool IsOurs(IGameObject gameObject)
    {
        if (!IsHuntGuard(gameObject))
        {
            return false;
        }

        var icon = ((NativeObject*)gameObject.Address)->NamePlateIconId;
        return icon is OwnGuardIcon or OwnGuardIconAlternate;
    }

    private static bool IsHuntGuard(IGameObject gameObject)
        => gameObject.Address != 0 && ((NativeObject*)gameObject.Address)->EventId.ContentId == EventHandlerContent.TreasureHuntDirector;

    private static bool IsLiveCombatant(IBattleNpc npc)
        => npc.BattleNpcKind == BattleNpcSubKind.Combatant && !npc.IsDead && npc.CurrentHp > 0;
}
