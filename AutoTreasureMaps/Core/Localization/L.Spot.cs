namespace AutoTreasureMaps.Core.Localization;

internal static partial class L
{
    internal static class Spot
    {
        public static readonly LocString Unresolved = new("spot.unresolved", "A deciphered map is held, but its dig spot could not be read. /atm spot writes the details to the console.");
        public static readonly LocString Coordinates = new("spot.coordinates", "X {0}  Y {1}");
        public static readonly LocString Party = new("spot.party", "Party");
        public static readonly LocString PartySolo = new("spot.partySolo", "Solo map, 1 player");
        public static readonly LocString PartyGroup = new("spot.partyGroup", "Up to {0} players");
        public static readonly LocString FullPartyHint = new("spot.fullPartyHint", "Tuned for a full party");
        public static readonly LocString Portal = new("spot.portal", "Portal");
        public static readonly LocString PortalAvailable = new("spot.portalAvailable", "Can open a portal");
        public static readonly LocString Flying = new("spot.flying", "Flying");
        public static readonly LocString FlyingUnlocked = new("spot.flyingUnlocked", "Unlocked in this zone");
        public static readonly LocString FlyingLocked = new("spot.flyingLocked", "Not unlocked in this zone yet");
        public static readonly LocString Flag = new("spot.flag", "Flag on map");
        public static readonly LocString FlagHint = new("spot.flagHint", "Place the map flag on the dig spot and open the map.");
        public static readonly LocString GoTo = new("spot.goto", "Go to spot");
        public static readonly LocString GoToHint = new("spot.gotoHint", "Travel to the dig spot and land on it.");

        public static readonly LocString ChatNone = new("spot.chat.none", "No deciphered map is held. Decipher a timeworn map first.");
        public static readonly LocString ChatHeld = new("spot.chat.held", "{0}: the dig spot is in {1}, X {2} Y {3}.");
        public static readonly LocString ChatUnresolved = new("spot.chat.unresolved", "The held map (rank {0}, spot {1}) has no readable dig spot. The console has the details.");
        public static readonly LocString ChatFlagged = new("spot.chat.flagged", "Map flag placed on the dig spot in {0}, X {1} Y {2}.");
        public static readonly LocString ChatFlagFailed = new("spot.chat.flagFailed", "Could not place the map flag right now.");
        public static readonly LocString ChatTiers = new("spot.chat.tiers", "Listed {0} map tiers in the console. /atm log opens it.");
    }
}
