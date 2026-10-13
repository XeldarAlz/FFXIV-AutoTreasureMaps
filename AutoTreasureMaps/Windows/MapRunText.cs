using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Treasure;
using AutoTreasureMaps.Windows.Sections;

namespace AutoTreasureMaps.Windows;

// The map run's live lines, shared by the running panel and the Current map card. Each is rebuilt only when what it
// shows changes, so drawing them every frame allocates nothing in between.
internal static class MapRunText
{
    private const int SecondsPerMinute = 60;

    private static CachedText step;
    private static CachedText fightStep;
    private static CachedText fight;
    private static CachedText huntTime;
    private static LanguageInfo? lastMapLanguage;
    private static MapResult lastMapFor;
    private static string lastMapLine = string.Empty;

    // The phase, with the wave and the guards left while the fight is on.
    public static string Step(TreasureProgress progress)
    {
        var phase = progress.Phase;
        if (phase != TreasurePhase.Fighting)
        {
            return step.Get((long)phase, static key => (TreasurePhase)key == TreasurePhase.Paused ? Loc.T(L.Run.PhasePaused) : ReadyState.PhaseLabel((TreasurePhase)key));
        }

        return fightStep.Get(FightKey(progress), static key => string.Concat(ReadyState.PhaseLabel(TreasurePhase.Fighting), TextDraw.Separator, FightLine(key)));
    }

    // Wave and guards left alone, empty outside the fight.
    public static string Fight(TreasureProgress progress)
        => progress.Phase == TreasurePhase.Fighting ? fight.Get(FightKey(progress), static key => FightLine(key)) : string.Empty;

    public static bool HasHuntTime(TreasureProgress progress) => TreasureHuntDirector.SecondsUntil(progress.HuntEndsAt) >= 0;

    public static string HuntTime(TreasureProgress progress)
    {
        var left = TreasureHuntDirector.SecondsUntil(progress.HuntEndsAt);
        return huntTime.Get(left, static key => key < 0
            ? Loc.T(L.MapRun.HuntTimeUnknown)
            : Loc.T(L.MapRun.HuntTimeLeft, string.Concat((key / SecondsPerMinute).ToString(Loc.Culture), ":", (key % SecondsPerMinute).ToString("D2", Loc.Culture))));
    }

    public static string Outcome(MapResult? result)
        => result is { } last ? Loc.T(MapOutcomes.Label(last.Outcome)) : Loc.T(L.MapRun.LastMapNone);

    // Tier, outcome, gains, time taken and a seen portal, for the line under the Current map card.
    public static string LastMap(in MapResult result)
    {
        if (result == lastMapFor && ReferenceEquals(lastMapLanguage, Loc.Current) && lastMapLine.Length > 0)
        {
            return lastMapLine;
        }

        lastMapFor = result;
        lastMapLanguage = Loc.Current;
        var parts = new List<string>(6) { result.HasTier ? result.Tier.Name : Loc.T(MapOutcomes.Cause(result)) };
        if (result.HasTier || result.Refusal != MapRefusal.None)
        {
            parts.Add(Loc.T(MapOutcomes.Label(result.Outcome)));
        }

        if (result.Gil > 0)
        {
            parts.Add(Loc.T(L.MapRun.Gil, result.Gil.ToString("N0", Loc.Culture)));
        }

        if (result.ItemCount > 0)
        {
            parts.Add(Loc.Plural(L.MapRun.Items, result.ItemCount));
        }

        if (result.Duration > TimeSpan.Zero)
        {
            parts.Add(Formatting.Elapsed(result.Duration));
        }

        if (result.PortalSeen)
        {
            parts.Add(Loc.T(L.MapRun.PortalSeen));
        }

        lastMapLine = string.Join(TextDraw.Separator, parts);
        return lastMapLine;
    }

    private static long FightKey(TreasureProgress progress) => ((long)progress.Wave << 32) | (uint)progress.GuardsLeft;

    private static string FightLine(long key)
        => string.Concat(Loc.T(L.MapRun.Wave, (int)(key >> 32)), TextDraw.Separator, Loc.Plural(L.MapRun.GuardsLeft, (int)(key & uint.MaxValue)));
}
