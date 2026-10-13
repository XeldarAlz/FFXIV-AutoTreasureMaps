using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Localization;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;

namespace AutoTreasureMaps.Core.Gather;

// The gearset worn before a daily map gather put the character on GathererClassJobId, and the character it belongs to.
internal readonly record struct GearsetReturn(int GearsetIndex, uint ClassJobId, uint GathererClassJobId, ulong ContentId);

// The way back from the gatherer. A gather switches back itself, but one cut short by a stop, a pause, a watchdog, a
// fault or a fight can leave the character on the gatherer, as the game refuses the change in combat or at a node. The
// switch is then asked for again whenever no task runs and the character is free. The return is dropped once the
// character is off that gatherer, by the switch or by the player's own hand, on another character, after a logout, and
// once the wait or the tries run out, so a later session is never switched unasked. Framework thread only.
internal static class GatherGearsetReturn
{
    private const long HeldByTask = long.MaxValue;
    private const int RetryMs = 3_000;
    // Room for a fight to end or a knocked-out gatherer to get back on its feet.
    private const long GiveUpMs = 5 * TimeUnits.MillisecondsPerMinute;
    // A free character the game still refuses has a lasting problem, such as a full Armoury Chest.
    private const int MaxTries = 10;

    private static GearsetReturn? pending;
    private static long giveUpAtMs = HeldByTask;
    private static long nextTryAtMs;
    private static int tries;

    // What a gather about to switch to the gatherer goes back to: the gearset worn now, or the one an earlier gather that
    // left the character on a gatherer still owes. The calling task holds the return until it lets go.
    public static GearsetReturn Remember(uint gathererClassJobId)
    {
        var memory = TryGetOwed(out var owed)
            ? owed with { GathererClassJobId = gathererClassJobId }
            : new GearsetReturn(GearsetSwitcher.CurrentGearsetIndex(), GearsetSwitcher.CurrentClassJobId(), gathererClassJobId, Svc.PlayerState.ContentId);
        pending = memory;
        giveUpAtMs = HeldByTask;
        return memory;
    }

    public static void Done() => pending = null;

    // The task is done with it; from here on the switch is retried while the character is free, within the wait.
    public static void Release()
    {
        if (pending is null)
        {
            return;
        }

        var now = Environment.TickCount64;
        giveUpAtMs = now + GiveUpMs;
        nextTryAtMs = now + RetryMs;
        tries = 0;
    }

    // True while the same character is still on the gatherer the gather put on, within the wait; anything else drops
    // the return. A character not loaded yet, between areas, keeps it without owing it for now.
    public static bool TryGetOwed(out GearsetReturn owed)
    {
        owed = default;
        if (pending is not { } candidate)
        {
            return false;
        }

        if (!Svc.ClientState.IsLoggedIn || Svc.PlayerState.ContentId != candidate.ContentId)
        {
            Drop("the character logged out or changed");
            return false;
        }

        var classJobId = GearsetSwitcher.CurrentClassJobId();
        if (classJobId == 0)
        {
            return false;
        }

        if (classJobId == candidate.ClassJobId)
        {
            Drop($"back on {GearsetSwitcher.ClassJobLabel(classJobId)}");
            return false;
        }

        if (classJobId != candidate.GathererClassJobId)
        {
            Drop($"the player picked {GearsetSwitcher.ClassJobLabel(classJobId)} since");
            return false;
        }

        if (Environment.TickCount64 >= giveUpAtMs)
        {
            GiveUp(candidate, $"still on {GearsetSwitcher.ClassJobLabel(classJobId)} {GiveUpMs / TimeUnits.MillisecondsPerMinute} min after the gather");
            return false;
        }

        owed = candidate;
        return true;
    }

    // Closes the gathering window first, as the game refuses the change while it is open. True when the request went out.
    public static bool Ask(in GearsetReturn owed, string reason)
    {
        GatherOps.Close();
        var asked = GearsetSwitcher.EquipWithoutWaiting(owed.ClassJobId, owed.GearsetIndex);
        RunLog.Info(asked
            ? $"Gather: asked the game to switch back to {GearsetSwitcher.ClassJobLabel(owed.ClassJobId)} ({reason})"
            : $"Gather: could not ask to switch back to {GearsetSwitcher.ClassJobLabel(owed.ClassJobId)} yet ({reason})");
        return asked;
    }

    public static void Tick()
    {
        if (pending is null || giveUpAtMs == HeldByTask || Environment.TickCount64 < nextTryAtMs)
        {
            return;
        }

        if (!TryGetOwed(out var owed) || clib.Services.Svc.Automation.Running || !Free())
        {
            return;
        }

        if (tries >= MaxTries)
        {
            GiveUp(owed, $"the game refused the switch {MaxTries} times");
            return;
        }

        tries++;
        nextTryAtMs = Environment.TickCount64 + RetryMs;
        Ask(owed, $"retry {tries}/{MaxTries}");
    }

    // An unload leaves no later frame to finish on, so the switch is asked for at once if it can be.
    public static void AskBeforeUnload()
    {
        if (!TryGetOwed(out var owed))
        {
            return;
        }

        if (!Ask(owed, "plugin unloading"))
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Gather.ReasonSwitchBackFailed)}");
        }

        pending = null;
    }

    private static bool Free()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.Casting]
        && !Svc.Condition[ConditionFlag.ExecutingGatheringAction]
        && !GenericHelpers.IsOccupied();

    private static void GiveUp(in GearsetReturn owed, string why)
    {
        RunLog.Warning($"Gather: gave up switching back to {GearsetSwitcher.ClassJobLabel(owed.ClassJobId)}: {why}");
        Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Gather.ReasonSwitchBackFailed)}");
        pending = null;
    }

    private static void Drop(string why)
    {
        RunLog.Info($"Gather: the switch back is no longer owed: {why}");
        pending = null;
    }
}
