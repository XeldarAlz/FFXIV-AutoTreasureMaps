using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AutoTreasureMaps.Core.Treasure;

// The director the game runs a dug-up coffer's hunt under, from the trap to the loot. The duty list's todo points at it
// while it shows, and the director module lists it either way. Read only on the framework thread.
internal static unsafe class TreasureHuntDirector
{
    // A countdown longer than this is not a hunt's time limit but a misread field.
    private const long MaxPlausibleSeconds = 3_600;

    public static bool IsActive() => Find() is not null;

    // The director's own start and end, server unix seconds. False while no hunt runs or the end is not a plausible limit.
    public static bool TryReadTimes(out long startTimestamp, out long endTimestamp)
    {
        startTimestamp = 0;
        endTimestamp = 0;
        var director = Find();
        if (director is null)
        {
            return false;
        }

        startTimestamp = director->DirectorStartTimestamp;
        endTimestamp = director->DirectorEndTimestamp;
        return endTimestamp > startTimestamp && endTimestamp - startTimestamp <= MaxPlausibleSeconds;
    }

    // Seconds until endTimestamp on the server's clock, 0 once it has passed, -1 when there is no plausible end.
    public static long SecondsUntil(long endTimestamp)
    {
        if (endTimestamp <= 0)
        {
            return -1;
        }

        var left = endTimestamp - ServerNow();
        if (left > MaxPlausibleSeconds)
        {
            return -1;
        }

        return Math.Max(0, left);
    }

    public static long ServerNow()
        => Framework.Addresses.GetServerTime.Value != 0 ? Framework.GetServerTime() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static Director* Find()
    {
        var state = UIState.Instance();
        if (state is not null && IsTreasureHunt(state->DirectorTodo.Director))
        {
            return state->DirectorTodo.Director;
        }

        var framework = EventFramework.Instance();
        if (framework is null)
        {
            return null;
        }

        ref var directors = ref framework->DirectorModule.DirectorList;
        var count = directors.LongCount;
        for (long index = 0; index < count; index++)
        {
            var director = directors[index].Value;
            if (IsTreasureHunt(director))
            {
                return director;
            }
        }

        return null;
    }

    private static bool IsTreasureHunt(Director* director)
        => director is not null && director->Info.EventId.ContentId == EventHandlerContent.TreasureHuntDirector;
}
