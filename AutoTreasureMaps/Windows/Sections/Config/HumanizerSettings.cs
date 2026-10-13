using AutoTreasureMaps.Core;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Travel;
using AutoTreasureMaps.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTreasureMaps.Windows.Sections.Config;

internal static class HumanizerSettings
{
    private const int MapsMin = 1;
    private const int MapsMax = 200;
    private const int BreakMinutesMin = 1;
    private const int BreakMinutesMax = 60;
    private const int PauseSecondsMax = 60;
    private const int WanderMetersMin = 5;
    private const int WanderMetersMax = 200;
    private const string CityToggleId = "##atm_humanizer_city";
    private const float RangeStepperWidth = 118f;
    private const float RangeGap = 8f;

    private readonly record struct DelayRow(HumanAction Action, LocString Label, LocString Help, string MinId, string MaxId);

    private static readonly DelayRow[] delayRows =
    [
        new(HumanAction.MoveOff, L.Humanizer.MoveOff, L.Humanizer.MoveOffHelp, "##atm_human_moveoff_min", "##atm_human_moveoff_max"),
        new(HumanAction.Teleport, L.Humanizer.Teleport, L.Humanizer.TeleportHelp, "##atm_human_teleport_min", "##atm_human_teleport_max"),
        new(HumanAction.Dig, L.Humanizer.Dig, L.Humanizer.DigHelp, "##atm_human_dig_min", "##atm_human_dig_max"),
        new(HumanAction.OpenCoffer, L.Humanizer.OpenCoffer, L.Humanizer.OpenCofferHelp, "##atm_human_coffer_min", "##atm_human_coffer_max"),
        new(HumanAction.Engage, L.Humanizer.Engage, L.Humanizer.EngageHelp, "##atm_human_engage_min", "##atm_human_engage_max"),
    ];

    public static void Draw(Configuration configuration)
    {
        DrawDelaysGroup(configuration);
        DrawBreaksGroup(configuration);
        using var more = Motion.PushSection("##atm_humanizer_more", configuration.CityBreaksEnabled);
        if (more is null)
        {
            return;
        }

        DrawWanderingGroup(configuration);
        DrawCitiesGroup(configuration);
    }

    private static void DrawDelaysGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Humanizer.Group));

        SettingsRow.Draw(Loc.T(L.Humanizer.Enabled),
            Loc.T(L.Humanizer.EnabledHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(configuration, () => configuration.ReactionDelaysEnabled, value => configuration.ReactionDelaysEnabled = value, "##atm_human_enabled"),
            SettingsRow.ToggleHeight);

        using var delays = Motion.PushSection("##atm_human_delays", configuration.ReactionDelaysEnabled);
        if (delays is null)
        {
            return;
        }

        var controlWidth = RangeWidth();
        for (var index = 0; index < delayRows.Length; index++)
        {
            var row = delayRows[index];
            SettingsRow.Draw(Loc.T(row.Label), Loc.T(row.Help), controlWidth, () => DrawDelayRange(configuration, row));
        }
    }

    // In unscaled units, as SettingsRow scales the control width itself.
    private static float RangeWidth()
        => RangeStepperWidth * 2f + RangeGap * 2f + TextDraw.Measure(Loc.T(L.Humanizer.RangeTo)).X / ImGuiHelpers.GlobalScale;

    private static void DrawDelayRange(Configuration configuration, in DelayRow row)
    {
        var gap = RangeGap * ImGuiHelpers.GlobalScale;
        var format = Loc.T(L.Humanizer.SecondsFormat);
        var range = configuration.DelayFor(row.Action);
        var shortest = range.MinSeconds;
        var longest = range.MaxSeconds;

        var changed = Stepper.Draw(row.MinId, ref shortest, Humanizer.SecondsStep, Humanizer.SecondsMin, Humanizer.SecondsMax, format, RangeStepperWidth);
        ImGui.SameLine(0f, gap);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted(Loc.T(L.Humanizer.RangeTo));
        }

        ImGui.SameLine(0f, gap);
        changed |= Stepper.Draw(row.MaxId, ref longest, Humanizer.SecondsStep, Humanizer.SecondsMin, Humanizer.SecondsMax, format, RangeStepperWidth);
        if (!changed)
        {
            return;
        }

        configuration.SetDelay(row.Action, new DelayRange(shortest, longest));
        configuration.SaveDebounced();
    }

    private static void DrawBreaksGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Safety.HumanizerBreaks));

        SettingsRow.Draw(Loc.T(L.Safety.HumanizerEnable),
            Loc.T(L.Safety.HumanizerEnableHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(configuration, () => configuration.CityBreaksEnabled, value => configuration.CityBreaksEnabled = value, "##atm_humanizer_on"),
            SettingsRow.ToggleHeight);

        using var body = Motion.PushSwitch("##atm_humanizer_body", configuration.CityBreaksEnabled);
        if (!configuration.CityBreaksEnabled)
        {
            SettingsRow.Note(Loc.T(L.Safety.HumanizerOff));
            return;
        }

        SettingsRow.Draw(Loc.T(L.Safety.MapsBetween),
            Loc.T(L.Safety.MapsBetweenHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(configuration, "##atm_humanizer_maps",
                () => configuration.HumanizerMapsBeforeBreak, value => configuration.HumanizerMapsBeforeBreak = value,
                MapsMin, MapsMax, Loc.T(L.Safety.MapsFormat)));

        SettingsRow.Draw(Loc.T(L.Safety.BreakLength),
            Loc.T(L.Safety.BreakLengthHelp),
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(configuration, "##atm_humanizer_break_min", "##atm_humanizer_break_max",
                () => configuration.HumanizerBreakMinMinutes, value => configuration.HumanizerBreakMinMinutes = value,
                () => configuration.HumanizerBreakMaxMinutes, value => configuration.HumanizerBreakMaxMinutes = value,
                BreakMinutesMax, BreakMinutesMin, Loc.T(L.Safety.MinutesFormat)));
    }

    private static void DrawWanderingGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Safety.HumanizerWandering));

        SettingsRow.Draw(Loc.T(L.Safety.PauseBetween),
            Loc.T(L.Safety.PauseBetweenHelp),
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(configuration, "##atm_humanizer_pause_min", "##atm_humanizer_pause_max",
                () => configuration.HumanizerPauseMinSeconds, value => configuration.HumanizerPauseMinSeconds = value,
                () => configuration.HumanizerPauseMaxSeconds, value => configuration.HumanizerPauseMaxSeconds = value,
                PauseSecondsMax, 0, Loc.T(L.Safety.SecondsFormat)));

        SettingsRow.Draw(Loc.T(L.Safety.WalkDistance),
            Loc.T(L.Safety.WalkDistanceHelp),
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(configuration, "##atm_humanizer_wander_min", "##atm_humanizer_wander_max",
                () => configuration.HumanizerWanderMinMeters, value => configuration.HumanizerWanderMinMeters = value,
                () => configuration.HumanizerWanderMaxMeters, value => configuration.HumanizerWanderMaxMeters = value,
                WanderMetersMax, WanderMetersMin, Loc.T(L.Safety.MetersFormat)));
    }

    private static void DrawCitiesGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Safety.HumanizerCities));

        SettingsRow.DrawBlock(Loc.T(L.Safety.AllowedCities),
            Loc.T(L.Safety.AllowedCitiesHelp),
            () => DrawCityList(configuration));
    }

    // The list is ordered by expansion, so a header goes in wherever the expansion changes.
    private static void DrawCityList(Configuration configuration)
    {
        var cities = BreakCities.All;
        var anySelected = false;
        for (var index = 0; index < cities.Length; index++)
        {
            var city = cities[index];
            if (index == 0 || cities[index - 1].Expansion != city.Expansion)
            {
                if (index > 0)
                {
                    ImGui.Spacing();
                }

                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                {
                    ImGui.TextUnformatted(ExpansionLabels.Name(city.Expansion));
                }
            }

            anySelected |= DrawCityToggle(configuration, city.TerritoryId);
        }

        ImGui.Spacing();
        if (anySelected)
        {
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
        {
            ImGui.TextWrapped(Loc.T(L.Safety.NoCities));
        }
    }

    private static bool DrawCityToggle(Configuration configuration, uint territoryId)
    {
        var selected = configuration.HumanizerCities.Contains(territoryId);
        ImGui.PushID((int)territoryId);
        var changed = ToggleSwitch.Draw(CityToggleId, ref selected);
        ImGui.PopID();
        if (changed)
        {
            if (selected)
            {
                configuration.HumanizerCities.Add(territoryId);
            }
            else
            {
                configuration.HumanizerCities.Remove(territoryId);
            }

            configuration.SaveDebounced();
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted(TerritoryNames.Of(territoryId));
        }

        return selected;
    }
}
