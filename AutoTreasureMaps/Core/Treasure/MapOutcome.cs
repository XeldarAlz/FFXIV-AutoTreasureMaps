using AutoTreasureMaps.Core.Localization;

namespace AutoTreasureMaps.Core.Treasure;

internal enum MapOutcome : byte
{
    Looted,
    TimedOut,
    Died,
    DigFailed,
    Unreachable,
    Refused,
    Cancelled,
    Faulted,
}

// Why a map was refused, before it started or by the game's answer to the dig. None for every other outcome.
internal enum MapRefusal : byte
{
    None,
    NotLoggedIn,
    RunActive,
    TaskRunning,
    NeedsPlugins,
    NoMap,
    Unresolved,
    PortalMap,
    Undersea,
    NotUnlocked,
    InDuty,
    PartyNotInArea,
    BoundByDuty,
    PartyBoundByDuty,
    PortalDug,
    CofferPrompt,
}

internal static class MapOutcomes
{
    public static ReadOnlySpan<uint> DigRefusalMessages =>
    [
        TreasureLogMessages.DigPartyNotInArea,
        TreasureLogMessages.DigBoundByDuty,
        TreasureLogMessages.DigPartyBoundByDuty,
    ];

    public static MapRefusal FromDigMessage(uint logMessageId) => logMessageId switch
    {
        TreasureLogMessages.DigPartyNotInArea => MapRefusal.PartyNotInArea,
        TreasureLogMessages.DigBoundByDuty => MapRefusal.BoundByDuty,
        TreasureLogMessages.DigPartyBoundByDuty => MapRefusal.PartyBoundByDuty,
        _ => MapRefusal.None,
    };

    // A map that ended any other way is finished; a cancelled or faulted one may still be resumed.
    public static bool Final(MapOutcome outcome) => outcome is not (MapOutcome.Cancelled or MapOutcome.Faulted);

    public static LocString Label(MapOutcome outcome) => outcome switch
    {
        MapOutcome.Looted => L.MapRun.OutcomeLooted,
        MapOutcome.TimedOut => L.MapRun.OutcomeTimedOut,
        MapOutcome.Died => L.MapRun.OutcomeDied,
        MapOutcome.DigFailed => L.MapRun.OutcomeDigFailed,
        MapOutcome.Unreachable => L.MapRun.OutcomeUnreachable,
        MapOutcome.Refused => L.MapRun.OutcomeRefused,
        MapOutcome.Cancelled => L.MapRun.OutcomeCancelled,
        _ => L.MapRun.OutcomeFaulted,
    };

    // What a result with no tier tells: why it was refused, or how it ended when it failed before its map was read.
    public static LocString Cause(in MapResult result) => result.Refusal == MapRefusal.None ? Label(result.Outcome) : Reason(result.Refusal);

    public static LocString Reason(MapRefusal refusal) => refusal switch
    {
        MapRefusal.NotLoggedIn => L.Decipher.NotLoggedIn,
        MapRefusal.RunActive => L.Decipher.RunActive,
        MapRefusal.TaskRunning => L.Decipher.TaskRunning,
        MapRefusal.NeedsPlugins => L.Goto.NeedsPlugins,
        MapRefusal.NoMap => L.Spot.ChatNone,
        MapRefusal.Unresolved => L.MapRun.RefusedUnresolved,
        MapRefusal.PortalMap => L.MapRun.RefusedPortalMap,
        MapRefusal.Undersea => L.MapRun.RefusedUndersea,
        MapRefusal.NotUnlocked => L.MapRun.RefusedNotUnlocked,
        MapRefusal.InDuty => L.MapRun.RefusedInDuty,
        MapRefusal.PartyNotInArea => L.MapRun.RefusedPartyNotInArea,
        MapRefusal.BoundByDuty => L.MapRun.RefusedBoundByDuty,
        MapRefusal.PartyBoundByDuty => L.MapRun.RefusedPartyBoundByDuty,
        MapRefusal.PortalDug => L.MapRun.RefusedPortalDug,
        _ => L.MapRun.RefusedCofferPrompt,
    };
}
