using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Numerics;

namespace AutoTreasureMaps.Core.Game;

// Nothing in the client flags a deciphered map's spot; using the map only shows a picture of the area. So the flag is
// placed here, the same way a chat map link does, and the map is opened on it.
internal static unsafe class MapFlag
{
    public static bool PlaceAndOpen(uint territoryId, uint mapId, Vector3 worldPosition, string windowTitle)
    {
        var agent = AgentMap.Instance();
        if (agent is null)
        {
            return false;
        }

        agent->SetFlagMapMarker(territoryId, mapId, worldPosition);
        agent->OpenMap(mapId, territoryId, windowTitle, MapType.FlagMarker);
        return true;
    }
}
