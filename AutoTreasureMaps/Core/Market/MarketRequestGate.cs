namespace AutoTreasureMaps.Core.Market;

// Shared by every visit and the manual buy, so a request right after another one still waits its turn.
internal static class MarketRequestGate
{
    // The server's window is not documented; tools that request listings in a loop keep 2 to 2.5 s between requests.
    public const int SpacingMs = 2_500;

    private static long lastRequestAtMs;

    public static int WaitMs()
        => (int)Math.Clamp(lastRequestAtMs + SpacingMs - Environment.TickCount64, 0L, SpacingMs);

    public static void NoteRequest() => lastRequestAtMs = Environment.TickCount64;
}
