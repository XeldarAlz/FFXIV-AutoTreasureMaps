using AutoTreasureMaps.Core.Hunts;
using AutoTreasureMaps.Core.Localization;
using Dalamud.Interface;

namespace AutoTreasureMaps.Windows;

internal static class HuntModeLabels
{
    public static FontAwesomeIcon Icon(HuntMode mode) => mode switch
    {
        HuntMode.HuntingLog => FontAwesomeIcon.BookOpen,
        HuntMode.CustomList => FontAwesomeIcon.Crosshairs,
        HuntMode.RankFarm => FontAwesomeIcon.Crosshairs,
        _ => FontAwesomeIcon.Scroll,
    };

    public static string Label(HuntMode mode) => Loc.T(mode switch
    {
        HuntMode.HuntingLog => L.HuntingLog.ModeHuntingLog,
        HuntMode.CustomList => L.HuntingLog.ModeCustom,
        HuntMode.RankFarm => L.RankFarm.Mode,
        _ => L.HuntingLog.ModeBills,
    });
}
