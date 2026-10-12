using AutoTreasureMaps.Core.Travel;

namespace AutoTreasureMaps.Core.Treasure;

// One row of the game's TreasureHuntRank sheet. KeyItemId is the deciphered map, PortalKeyItemId the thief's map a
// tier hands out when its portal could not be entered (0 for a tier without a portal).
internal readonly record struct TreasureTier(
    string Name,
    uint TimewornItemId,
    uint KeyItemId,
    uint PortalKeyItemId,
    byte Rank,
    byte MaxPartySize,
    byte SpotCount,
    ExpansionKind Expansion,
    bool Undersea,
    bool Tradable)
{
    private const byte FullPartySize = 8;

    public bool HasPortal => PortalKeyItemId != 0;

    public bool TunedForFullParty => MaxPartySize >= FullPartySize;
}
