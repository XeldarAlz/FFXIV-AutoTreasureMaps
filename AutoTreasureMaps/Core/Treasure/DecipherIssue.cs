using AutoTreasureMaps.Core.Localization;

namespace AutoTreasureMaps.Core.Treasure;

internal enum DecipherRoute : byte
{
    Menu,
    Direct,
}

internal enum DecipherIssue : byte
{
    None,
    NotLoggedIn,
    Deciphering,
    TaskRunning,
    RunActive,
    MapHeld,
    NotUnlocked,
    InDuty,
    Mounted,
    PlayerBusy,
    NoMaps,
    NoEnabledMaps,
    ListNotShown,
    MapNotListed,
    PromptNotShown,
    PromptMismatch,
    NotConfirmed,
    Refused,
    Cancelled,
}

internal static class DecipherIssues
{
    public static ReadOnlySpan<uint> FailureMessages =>
    [
        TreasureLogMessages.AlreadyHoldingMap,
        TreasureLogMessages.NoDecipherableMap,
        TreasureLogMessages.DecipherBoundByDuty,
        TreasureLogMessages.CannotExecute,
    ];

    public static DecipherIssue FromMessage(uint logMessageId) => logMessageId switch
    {
        TreasureLogMessages.AlreadyHoldingMap => DecipherIssue.MapHeld,
        TreasureLogMessages.NoDecipherableMap => DecipherIssue.NoMaps,
        TreasureLogMessages.DecipherBoundByDuty => DecipherIssue.InDuty,
        _ => DecipherIssue.Refused,
    };

    // A window that never opened or a confirmation that never came may be a dropped click; the game's own refusals
    // and a prompt naming another map would only repeat.
    public static bool Retryable(DecipherIssue issue)
        => issue is DecipherIssue.ListNotShown or DecipherIssue.MapNotListed or DecipherIssue.PromptNotShown
            or DecipherIssue.NotConfirmed or DecipherIssue.Refused;

    public static LocString Reason(DecipherIssue issue) => issue switch
    {
        DecipherIssue.NotLoggedIn => L.Decipher.NotLoggedIn,
        DecipherIssue.Deciphering => L.Decipher.Deciphering,
        DecipherIssue.TaskRunning => L.Decipher.TaskRunning,
        DecipherIssue.RunActive => L.Decipher.RunActive,
        DecipherIssue.MapHeld => L.Decipher.MapHeld,
        DecipherIssue.NotUnlocked => L.Decipher.NotUnlocked,
        DecipherIssue.InDuty => L.Decipher.InDuty,
        DecipherIssue.Mounted => L.Decipher.Mounted,
        DecipherIssue.PlayerBusy => L.Decipher.PlayerBusy,
        DecipherIssue.NoMaps => L.Decipher.NoMaps,
        DecipherIssue.NoEnabledMaps => L.Decipher.NoEnabledMaps,
        DecipherIssue.ListNotShown => L.Decipher.ListNotShown,
        DecipherIssue.MapNotListed => L.Decipher.MapNotListed,
        DecipherIssue.PromptNotShown => L.Decipher.PromptNotShown,
        DecipherIssue.PromptMismatch => L.Decipher.PromptMismatch,
        DecipherIssue.NotConfirmed => L.Decipher.NotConfirmed,
        DecipherIssue.Refused => L.Decipher.Refused,
        _ => L.Decipher.Cancelled,
    };
}
