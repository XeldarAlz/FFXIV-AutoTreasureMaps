using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Windows.Components;

namespace AutoTreasureMaps.Windows.Sections.Config;

internal static class DailyMapSettings
{
    private const int VisitsMin = 1;
    private const int VisitsMax = 20;

    private static readonly DailyMapGatherer[] gatherers = [DailyMapGatherer.Automatic, DailyMapGatherer.Miner, DailyMapGatherer.Botanist];

    // Ordered like gatherers, because the picked index reads both.
    private static readonly SettingsControls.Choices.Choice[] gathererChoices =
    [
        new(L.Gather.GathererAutomaticName, L.Gather.GathererAutomaticDetail),
        new(L.Gather.GathererMinerName, L.Gather.GathererMinerDetail),
        new(L.Gather.GathererBotanistName, L.Gather.GathererBotanistDetail),
    ];

    public static void Draw(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Gather.Group));

        SettingsRow.Draw(Loc.T(L.Gather.Enabled),
            Loc.T(L.Gather.EnabledHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(configuration, () => configuration.GatherDailyMap, value => configuration.GatherDailyMap = value, "##atm_gather_enabled"),
            SettingsRow.ToggleHeight);

        var selected = Math.Max(0, Array.IndexOf(gatherers, configuration.DailyMapGatherer));
        SettingsRow.Draw(Loc.T(L.Gather.Gatherer),
            Loc.T(L.Gather.GathererHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##atm_gather_gatherer", gathererChoices, selected, choice =>
            {
                configuration.DailyMapGatherer = gatherers[choice];
                configuration.SaveDebounced();
            }));
        SettingsRow.Caption(Loc.T(gathererChoices[selected].Detail));

        SettingsRow.Draw(Loc.T(L.Gather.Visits),
            Loc.T(L.Gather.VisitsHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(configuration, "##atm_gather_visits",
                () => configuration.DailyMapNodeVisits, value => configuration.DailyMapNodeVisits = value,
                VisitsMin, VisitsMax, Loc.T(L.Gather.VisitsFormat)));

        SettingsRow.Note(Loc.T(L.Gather.TierNote));
    }
}
