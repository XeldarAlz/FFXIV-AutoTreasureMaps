namespace AutoTreasureMaps;

public enum AfterRunAction
{
    StayLoggedIn,
    Logout,
    ReturnToInn,
    CloseGame,
}

public enum MapOrder
{
    HighestFirst,
    LowestFirst,
}

public enum LootRoll
{
    Need,
    Greed,
    Pass,
    Manual,
}

public enum DailyMapGatherer
{
    Automatic,
    Miner,
    Botanist,
}
