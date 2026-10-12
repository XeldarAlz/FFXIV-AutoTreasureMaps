namespace AutoTreasureMaps.Core.Localization;

internal static partial class L
{
    internal static class Decipher
    {
        public static readonly LocString NotLoggedIn = new("decipher.notLoggedIn", "Log in to a character first.");
        public static readonly LocString Deciphering = new("decipher.deciphering", "A map is being deciphered right now.");
        public static readonly LocString TaskRunning = new("decipher.taskRunning", "Another task is under way. Wait for it to finish.");
        public static readonly LocString RunActive = new("decipher.runActive", "Stop the run first.");
        public static readonly LocString MapHeld = new("decipher.mapHeld", "You already hold a deciphered map, and only one can be held at a time. Dig it up first.");
        public static readonly LocString NotUnlocked = new("decipher.notUnlocked", "Decipher is not unlocked on this character yet.");
        public static readonly LocString InDuty = new("decipher.inDuty", "Maps cannot be deciphered while you are bound by a duty.");
        public static readonly LocString Mounted = new("decipher.mounted", "Dismount first.");
        public static readonly LocString PlayerBusy = new("decipher.playerBusy", "You are busy right now: in combat, casting or in the middle of an action.");
        public static readonly LocString NoMaps = new("decipher.noMaps", "There is no timeworn map in your bags.");
        public static readonly LocString NoEnabledMaps = new("decipher.noEnabledMaps", "None of the maps in your bags is enabled. Settings, Maps chooses the tiers to use.");
        public static readonly LocString ListNotShown = new("decipher.listNotShown", "The game's map list did not open.");
        public static readonly LocString MapNotListed = new("decipher.mapNotListed", "The map was missing from the game's list.");
        public static readonly LocString PromptNotShown = new("decipher.promptNotShown", "The game's confirmation did not open.");
        public static readonly LocString PromptMismatch = new("decipher.promptMismatch", "The game's confirmation named a different map, so it was declined.");
        public static readonly LocString NotConfirmed = new("decipher.notConfirmed", "The game never confirmed the decipher.");
        public static readonly LocString Refused = new("decipher.refused", "The game refused the action right now.");
        public static readonly LocString Cancelled = new("decipher.cancelled", "Cancelled.");

        public static readonly LocString ChatStarted = new("decipher.chat.started", "Deciphering {0}.");
        public static readonly LocString ChatDone = new("decipher.chat.done", "Deciphered {0}.");
        public static readonly LocString ChatFailed = new("decipher.chat.failed", "Could not decipher {0}. {1}");
        public static readonly LocString ChatRefused = new("decipher.chat.refused", "Cannot decipher right now. {0}");
        public static readonly LocString ChatUnknownTier = new("decipher.chat.unknownTier", "No map tier matches “{0}”.");
        public static readonly LocString ChatNotInBags = new("decipher.chat.notInBags", "There is no {0} in your bags.");
    }
}
