using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Travel;

namespace AutoTreasureMaps.Windows;

internal static class ExpansionLabels
{
    public static string Name(ExpansionKind kind) => kind switch
    {
        ExpansionKind.ARR => Loc.T(L.Treasure.ExpansionArr),
        ExpansionKind.HW  => Loc.T(L.Treasure.ExpansionHw),
        ExpansionKind.SB  => Loc.T(L.Treasure.ExpansionSb),
        ExpansionKind.ShB => Loc.T(L.Treasure.ExpansionShb),
        ExpansionKind.EW  => Loc.T(L.Treasure.ExpansionEw),
        _                 => Loc.T(L.Treasure.ExpansionDt),
    };
}
