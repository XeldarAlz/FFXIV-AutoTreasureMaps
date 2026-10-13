using AutoTreasureMaps.Core;
using AutoTreasureMaps.Core.Gather;
using AutoTreasureMaps.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoTreasureMaps.Windows.Sections;

// The daily map allowance under the bag card. The wait is rebuilt once a minute, so drawing it every frame allocates
// only when the minute changes.
internal static class DailyMapLine
{
    private const float TopGap = 10f;
    private const float IconGap = 8f;

    private static CachedText waiting;

    public static float Height() => TopGap * ImGuiHelpers.GlobalScale + ImGui.GetTextLineHeight();

    public static void Draw(Plugin plugin, float left, float right, float top)
    {
        MapAllowance.NoteViewed();
        var (text, color) = Describe(plugin.Configuration);
        var scale = ImGuiHelpers.GlobalScale;
        var lineTop = top + TopGap * scale;
        var lineHeight = ImGui.GetTextLineHeight();
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Leaf);
        TextDraw.Icon(FontAwesomeIcon.Leaf, new Vector2(left, lineTop + (lineHeight - iconSize.Y) * 0.5f), color);
        var textX = left + iconSize.X + IconGap * scale;
        TextDraw.At(TextDraw.Truncate(text, right - textX), new Vector2(textX, lineTop), color);
    }

    private static (string Text, Vector4 Color) Describe(Configuration configuration)
    {
        if (!configuration.GatherDailyMap)
        {
            return (Loc.T(L.Gather.LineOff), Styling.TextMuted);
        }

        var reading = MapAllowance.Read();
        return reading.State switch
        {
            AllowanceState.Available => (Loc.T(L.Gather.LineAvailable), Styling.AccentMintSoft),
            AllowanceState.Waiting => (WaitingText(reading.SecondsLeft), Styling.TextSecondary),
            _ => (Loc.T(L.Gather.LineChecking), Styling.TextMuted),
        };
    }

    private static string WaitingText(long secondsLeft)
    {
        var minutes = (secondsLeft + TimeUnits.SecondsPerMinute - 1) / TimeUnits.SecondsPerMinute;
        return waiting.Get(minutes, static key => Loc.T(L.Gather.LineWaiting, DailyMapPlanner.Duration(key * TimeUnits.SecondsPerMinute)));
    }
}
