using ECommons.DalamudServices;

namespace AutoTreasureMaps.Core.Game.Player;

internal static class PartyPresence
{
    // The local party list only: a cross-world party is not in it and reads as present, so a caller still retries on a
    // timer of its own.
    public static bool AllInZone()
    {
        var party = Svc.Party;
        var territoryId = Svc.ClientState.TerritoryType;
        for (var index = 0; index < party.Length; index++)
        {
            if (party[index] is { } member && member.Territory.RowId != territoryId)
            {
                return false;
            }
        }

        return true;
    }
}
