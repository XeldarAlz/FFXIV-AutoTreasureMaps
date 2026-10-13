using AutoTreasureMaps.Core.Travel;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Numerics;

namespace AutoTreasureMaps.Core.Market;

internal enum MarketBoardRoute : byte
{
    Aetheryte,
    Aethernet,
    District,
}

// A city's board nearest the place a teleport sets the character down, read from the city's layout files. AetheryteId
// is the city aetheryte a trip there lands at; UnlockQuestId is 0 for a board open to everyone.
internal readonly record struct CityBoard(MarketBoardChoice Choice, uint TerritoryId, Vector3 Position, uint AetheryteId, uint UnlockQuestId, MarketBoardRoute Route);

internal static class MarketBoards
{
    // Every EObj named "market board". Only three are placed in a city, but each opens the same search window.
    public static readonly uint[] ObjectIds = [2000073, 2000402, 2000440, 2000442, 2010285];

    // Travelers of Norvrandt opens the Crystarium's board.
    private const uint CrystariumBoardQuestId = 68817;

    // The first entries are the boards about 40 yalms from their city aetheryte, with no aethernet ride on the way.
    private const int ReliableCount = 2;

    private static readonly CityBoard[] cities =
    [
        new(MarketBoardChoice.LimsaLominsa, 129, new Vector3(-118.97f, 17.99f, 20.95f), 8, 0, MarketBoardRoute.Aetheryte),
        new(MarketBoardChoice.OldSharlayan, 962, new Vector3(19.77f, 2.38f, -35.58f), 182, 0, MarketBoardRoute.Aetheryte),
        new(MarketBoardChoice.Tuliyollal, 1185, new Vector3(2.94f, -14.00f, 135.82f), 216, 0, MarketBoardRoute.Aethernet),
        new(MarketBoardChoice.Kugane, 628, new Vector3(2.50f, 4.00f, 47.96f), 111, 0, MarketBoardRoute.Aethernet),
        new(MarketBoardChoice.UlDah, 131, new Vector3(149.09f, 4.00f, -32.50f), 9, 0, MarketBoardRoute.District),
        new(MarketBoardChoice.Crystarium, 819, new Vector3(-49.76f, -7.49f, 132.16f), 133, CrystariumBoardQuestId, MarketBoardRoute.Aethernet),
        new(MarketBoardChoice.Ishgard, 419, new Vector3(-152.81f, -12.53f, -41.98f), 70, 0, MarketBoardRoute.District),
        new(MarketBoardChoice.Gridania, 133, new Vector3(150.89f, 15.78f, -135.66f), 2, 0, MarketBoardRoute.District),
    ];

    public static ReadOnlySpan<CityBoard> Cities => cities;

    // Automatic keeps to the reliable city the character stands in, else takes the first reliable one it can teleport to.
    public static CityBoard Resolve(MarketBoardChoice choice)
    {
        if (choice != MarketBoardChoice.Automatic)
        {
            for (var index = 0; index < cities.Length; index++)
            {
                if (cities[index].Choice == choice)
                {
                    return cities[index];
                }
            }
        }

        uint territoryId = Svc.ClientState.TerritoryType;
        for (var index = 0; index < ReliableCount; index++)
        {
            if (cities[index].TerritoryId == territoryId)
            {
                return cities[index];
            }
        }

        for (var index = 0; index < ReliableCount; index++)
        {
            if (ZoneAetherytes.IsAttuned(cities[index].AetheryteId))
            {
                return cities[index];
            }
        }

        return cities[0];
    }

    public static bool Reachable(in CityBoard board)
    {
        if (board.UnlockQuestId != 0 && !QuestManager.IsQuestComplete(board.UnlockQuestId))
        {
            return false;
        }

        return Svc.ClientState.TerritoryType == board.TerritoryId || ZoneAetherytes.IsAttuned(board.AetheryteId);
    }
}
