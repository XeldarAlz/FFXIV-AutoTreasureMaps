using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace AutoTreasureMaps.Core.Gather;

internal enum AllowanceState : byte
{
    Unknown,
    Available,
    Waiting,
}

// AvailableAt and ServerNow are unix seconds on the server's clock.
internal readonly record struct AllowanceReading(AllowanceState State, long AvailableAt, long ServerNow)
{
    public long SecondsLeft => Math.Max(0, AvailableAt - ServerNow);
}

// The next daily map allowance as the server last reported it. The game fills the field only on request, and every
// request is a packet to the server, so requests are rare: once after login, at a run's start, after a gather attempt,
// and at most once a minute while the window shows the map as available or no reply has come. Framework thread only.
internal static unsafe class MapAllowance
{
    // The game writes -1 into the field when it sends the request and the server's reply overwrites it.
    private const long Pending = -1;
    // The client's own Timers window calls the map available from five seconds before the reported time.
    private const long ServerSlackSeconds = 5;
    private const long NeverMs = long.MinValue / 2;
    // Callers that ask within this long of each other share one request.
    private const int MinimumRequestGapMs = 10_000;
    private const int RepeatRequestMs = 60_000;
    // The window counts as showing the allowance while it drew the line this recently.
    private const int ViewedWindowMs = 2_000;

    private static bool requestedThisLogin;
    // Set once a gather took today's map or the game refused one as over the day's limit. The field then counts as
    // unknown until a reply to a request sent after it comes in, so a stale reading cannot send the run to gather again.
    private static bool markedUsed;
    private static bool askedSinceMarkedUsed;
    private static long lastKnown = Pending;
    private static long lastRequestAtMs = NeverMs;
    private static long viewedAtMs = NeverMs;

    public static bool ReplyPending
    {
        get
        {
            var uiState = UIState.Instance();
            return uiState is null || uiState->NextMapAllowanceTimestamp == Pending;
        }
    }

    public static AllowanceReading Read()
    {
        var uiState = UIState.Instance();
        if (!requestedThisLogin || uiState is null)
        {
            return new AllowanceReading(AllowanceState.Unknown, 0, 0);
        }

        var timestamp = uiState->NextMapAllowanceTimestamp;
        if (markedUsed)
        {
            if (!askedSinceMarkedUsed || timestamp == Pending)
            {
                return new AllowanceReading(AllowanceState.Unknown, 0, 0);
            }

            markedUsed = false;
        }

        if (timestamp == Pending)
        {
            timestamp = lastKnown;
        }
        else
        {
            lastKnown = timestamp;
        }

        if (timestamp == Pending)
        {
            return new AllowanceReading(AllowanceState.Unknown, 0, 0);
        }

        var serverNow = Framework.GetServerTime();
        // Unsigned, as the game compares it: 0 and any past time both read as available.
        var available = (ulong)timestamp <= (ulong)(serverNow + ServerSlackSeconds);
        return new AllowanceReading(available ? AllowanceState.Available : AllowanceState.Waiting, timestamp, serverNow);
    }

    // True when a request went out; a request within the minimum gap of the last one is dropped.
    public static bool Request(string reason)
    {
        var uiState = UIState.Instance();
        if (uiState is null || !CanAsk())
        {
            return false;
        }

        var now = Environment.TickCount64;
        if (now - lastRequestAtMs < MinimumRequestGapMs)
        {
            return false;
        }

        lastRequestAtMs = now;
        requestedThisLogin = true;
        askedSinceMarkedUsed = markedUsed;
        uiState->RequestResetTimestamps();
        RunLog.Debug($"Daily map: asked the server for the next map allowance ({reason}).");
        return true;
    }

    public static void MarkUsed()
    {
        markedUsed = true;
        askedSinceMarkedUsed = false;
        lastKnown = Pending;
        RunLog.Debug("Daily map: today's map is gathered; the allowance counts as used until the server replies again.");
    }

    public static void NoteViewed() => viewedAtMs = Environment.TickCount64;

    public static void Tick(Configuration configuration)
    {
        if (!Svc.ClientState.IsLoggedIn)
        {
            Forget();
            return;
        }

        if (!configuration.GatherDailyMap)
        {
            return;
        }

        if (!requestedThisLogin)
        {
            Request("after login");
            return;
        }

        var now = Environment.TickCount64;
        if (now - lastRequestAtMs < RepeatRequestMs)
        {
            return;
        }

        // A reply that never came would keep the run from ever gathering, window open or not. A known wait only runs
        // down, so of the rest only an available allowance is worth asking about again, while the window shows it.
        var state = Read().State;
        if (state == AllowanceState.Unknown)
        {
            Request("no reply yet");
        }
        else if (state == AllowanceState.Available && now - viewedAtMs <= ViewedWindowMs)
        {
            Request("window open");
        }
    }

    private static bool CanAsk()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.BetweenAreas51];

    private static void Forget()
    {
        requestedThisLogin = false;
        markedUsed = false;
        askedSinceMarkedUsed = false;
        lastKnown = Pending;
    }
}
