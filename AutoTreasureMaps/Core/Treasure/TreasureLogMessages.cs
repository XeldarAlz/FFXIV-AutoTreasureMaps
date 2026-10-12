namespace AutoTreasureMaps.Core.Treasure;

// Rows of the game's LogMessage sheet that the treasure loop reacts to. Row ids are the same in every client language.
// Where a row's text switches between a coffer and a portal, its first parameter is 1 for the portal.
internal static class TreasureLogMessages
{
    public const uint CannotExecute = 579;
    public const uint ItemObtained = 1053;
    public const uint CofferEmpty = 1245;
    public const uint CofferOfAnotherPlayer = 1246;
    public const uint GatheringLimitReached = 3526;
    public const uint Deciphered = 3750;
    public const uint NothingToDigHere = 3751;
    public const uint HuntTimeExpired = 3752;
    public const uint HuntOffWithoutMap = 3753;
    public const uint CofferSealed = 3754;
    public const uint CofferUnlocked = 3755;
    public const uint TrapSprung = 3756;
    public const uint NoDecipherableMap = 3758;
    public const uint Discovered = 3759;
    public const uint DigWithoutMap = 3760;
    public const uint AlreadyDiscovered = 3761;
    public const uint DigPartyNotInArea = 3762;
    public const uint DigBoundByDuty = 3763;
    public const uint DigPartyBoundByDuty = 3764;
    public const uint GuardsDefeated = 3765;
    public const uint MapCrumbled = 3766;
    public const uint AlreadyHoldingMap = 3767;
    public const uint Reinforcements = 3768;
    public const uint NoMoreFinds = 3769;
    public const uint PortalAppeared = 3770;
    public const uint PortalNoResponse = 3771;
    public const uint CofferLocked = 3773;
    public const uint PortalHandPlaced = 3774;
    public const uint PortalPrepared = 3778;
    public const uint PortalDeactivated = 3779;
    public const uint DecipherBoundByDuty = 3788;
    public const uint MapCrumbledSecondRow = 4405;
    public const uint TrapSprungSecondRow = 9361;
    public const uint TrapSprungThirdRow = 9363;

    public static bool IsTracked(uint logMessageId) => NameOf(logMessageId) is not null;

    // Item rows fire for every item the player gets, so the watcher keeps them only when the item is a timeworn map.
    public static bool NamesAnItem(uint logMessageId) => logMessageId is ItemObtained or GatheringLimitReached;

    public static string? NameOf(uint logMessageId) => logMessageId switch
    {
        CannotExecute => nameof(CannotExecute),
        ItemObtained => nameof(ItemObtained),
        CofferEmpty => nameof(CofferEmpty),
        CofferOfAnotherPlayer => nameof(CofferOfAnotherPlayer),
        GatheringLimitReached => nameof(GatheringLimitReached),
        Deciphered => nameof(Deciphered),
        NothingToDigHere => nameof(NothingToDigHere),
        HuntTimeExpired => nameof(HuntTimeExpired),
        HuntOffWithoutMap => nameof(HuntOffWithoutMap),
        CofferSealed => nameof(CofferSealed),
        CofferUnlocked => nameof(CofferUnlocked),
        TrapSprung => nameof(TrapSprung),
        NoDecipherableMap => nameof(NoDecipherableMap),
        Discovered => nameof(Discovered),
        DigWithoutMap => nameof(DigWithoutMap),
        AlreadyDiscovered => nameof(AlreadyDiscovered),
        DigPartyNotInArea => nameof(DigPartyNotInArea),
        DigBoundByDuty => nameof(DigBoundByDuty),
        DigPartyBoundByDuty => nameof(DigPartyBoundByDuty),
        GuardsDefeated => nameof(GuardsDefeated),
        MapCrumbled => nameof(MapCrumbled),
        AlreadyHoldingMap => nameof(AlreadyHoldingMap),
        Reinforcements => nameof(Reinforcements),
        NoMoreFinds => nameof(NoMoreFinds),
        PortalAppeared => nameof(PortalAppeared),
        PortalNoResponse => nameof(PortalNoResponse),
        CofferLocked => nameof(CofferLocked),
        PortalHandPlaced => nameof(PortalHandPlaced),
        PortalPrepared => nameof(PortalPrepared),
        PortalDeactivated => nameof(PortalDeactivated),
        DecipherBoundByDuty => nameof(DecipherBoundByDuty),
        MapCrumbledSecondRow => nameof(MapCrumbledSecondRow),
        TrapSprungSecondRow => nameof(TrapSprungSecondRow),
        TrapSprungThirdRow => nameof(TrapSprungThirdRow),
        _ => null,
    };
}
