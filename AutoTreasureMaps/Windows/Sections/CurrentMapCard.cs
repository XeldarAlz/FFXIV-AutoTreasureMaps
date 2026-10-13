using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Travel;
using AutoTreasureMaps.Core.Treasure;
using AutoTreasureMaps.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoTreasureMaps.Windows.Sections;

// The composed lines are rebuilt only when the held map, its flight access or the language changes, so drawing the card
// every frame allocates nothing.
internal static class CurrentMapCard
{
    private const float PadX = 18f;
    private const float PadY = 16f;
    private const float EmblemSize = 46f;
    private const float EmblemGap = 14f;
    private const float TitleGap = 3f;
    private const float BadgeGap = 10f;
    private const float SectionGap = 12f;
    private const float FactRowHeight = 26f;
    private const float FactIconColumn = 26f;
    private const float FactLabelGap = 14f;
    private const float ButtonGap = 8f;
    private const int LiveFactCount = 2;

    private static Identity cachedIdentity;
    private static LanguageInfo? cachedLanguage;
    private static string location = string.Empty;
    private static string party = string.Empty;
    private static string flying = string.Empty;

    private readonly record struct Identity(byte Rank, ushort SpotIndex, bool FlyingUnlocked);

    // The held map, with the buttons that act on it. runBlocker is why the map cannot be run right now, None when it can.
    public static void Draw(in TreasureTier tier, in DigSpot spot, MapRefusal runBlocker, bool setupNeeded, bool runRunning)
        => DrawCard(tier, spot, null, runBlocker, setupNeeded, runRunning);

    // The map a run is on, with its step and the hunt's time left in place of the buttons.
    public static void DrawLive(TreasureProgress progress)
        => DrawCard(progress.Tier, progress.Spot, progress, MapRefusal.RunActive, setupNeeded: false, runRunning: true);

    private static void DrawCard(in TreasureTier tier, in DigSpot spot, TreasureProgress? live, MapRefusal runBlocker, bool setupNeeded, bool runRunning)
    {
        Compose(tier, spot);
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;

        float titleHeight;
        using (Fonts.PushHeadline())
        {
            titleHeight = ImGui.GetTextLineHeight();
        }

        var lineHeight = ImGui.GetTextLineHeight();
        var headerHeight = MathF.Max(EmblemSize * scale, titleHeight + TitleGap * scale + lineHeight);
        var rowHeight = MathF.Max(FactRowHeight * scale, lineHeight);
        var factCount = (tier.HasPortal ? 3 : 2) + (live is null ? 0 : LiveFactCount);
        var buttonHeight = live is null ? Layout.ActionPillHeight * scale : 0f;
        var sectionGap = SectionGap * scale;
        var buttonGap = live is null ? sectionGap : 0f;
        var padY = PadY * scale;
        var size = new Vector2(width, padY + headerHeight + sectionGap * 2f + rowHeight * factCount + buttonGap + buttonHeight + padY);
        var end = origin + size;
        Paint.Glass(drawList, origin, end, Styling.PanelRounding * scale, Styling.AccentNebula, 0.06f, 0f, elevated: true);

        var left = origin.X + PadX * scale;
        var right = end.X - PadX * scale;
        var y = origin.Y + padY;
        DrawHeader(drawList, tier, left, right, y, headerHeight, titleHeight, lineHeight);
        y += headerHeight + sectionGap;
        Paint.Hairline(drawList, new Vector2(left, y), new Vector2(right, y));
        y += sectionGap;
        y = DrawFacts(tier, spot, live, left, right, y, rowHeight);
        if (live is null)
        {
            DrawButtons(tier, spot, right, y + sectionGap, buttonHeight, runBlocker, setupNeeded, runRunning);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawHeader(ImDrawListPtr drawList, in TreasureTier tier, float left, float right, float top, float headerHeight, float titleHeight, float lineHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var emblemRadius = EmblemSize * scale * 0.5f;
        var emblemCenter = new Vector2(left + emblemRadius, top + headerHeight * 0.5f);
        ProgressRing.Disc(emblemCenter, emblemRadius, Styling.WithAlpha(Styling.AccentNebula, 0.18f));
        drawList.AddCircle(emblemCenter, emblemRadius, Paint.Col(Styling.WithAlpha(Styling.AccentNebula, 0.55f)), 0, 1f);
        ProgressRing.CenterIcon(emblemCenter, FontAwesomeIcon.MapMarkedAlt, Styling.AccentGlowSoft, emblemRadius * 0.85f);

        var textX = emblemCenter.X + emblemRadius + EmblemGap * scale;
        var textTop = top + (headerHeight - titleHeight - TitleGap * scale - lineHeight) * 0.5f;
        var badgeWidth = Badge.Draw(drawList, ExpansionLabels.Name(tier.Expansion), Styling.AccentNebula, right, textTop + titleHeight * 0.5f);
        using (Fonts.PushHeadline())
        {
            TextDraw.At(TextDraw.Truncate(tier.Name, right - badgeWidth - BadgeGap * scale - textX), new Vector2(textX, textTop), Styling.TextStrong);
        }

        TextDraw.At(TextDraw.Truncate(location, right - textX), new Vector2(textX, textTop + titleHeight + TitleGap * scale), Styling.TextDim);
    }

    private static float DrawFacts(in TreasureTier tier, in DigSpot spot, TreasureProgress? live, float left, float right, float top, float rowHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var partyLabel = Loc.T(L.Spot.Party);
        var portalLabel = Loc.T(L.Spot.Portal);
        var flyingLabel = Loc.T(L.Spot.Flying);
        var stepLabel = Loc.T(L.MapRun.FactStep);
        var huntTimeLabel = Loc.T(L.MapRun.FactHuntTime);
        var labelWidth = MathF.Max(TextDraw.SmallCapsSize(partyLabel).X, MathF.Max(TextDraw.SmallCapsSize(portalLabel).X, TextDraw.SmallCapsSize(flyingLabel).X));
        if (live is not null)
        {
            labelWidth = MathF.Max(labelWidth, MathF.Max(TextDraw.SmallCapsSize(stepLabel).X, TextDraw.SmallCapsSize(huntTimeLabel).X));
        }

        var valueX = left + FactIconColumn * scale + labelWidth + FactLabelGap * scale;

        var y = top;
        if (live is not null)
        {
            DrawFact(FontAwesomeIcon.ShoePrints, stepLabel, MapRunText.Step(live), Styling.AccentGlowSoft, null, left, valueX, right, y, rowHeight);
            y += rowHeight;
            var huntTimeColor = MapRunText.HasHuntTime(live) ? Styling.TextSecondary : Styling.TextMuted;
            DrawFact(FontAwesomeIcon.HourglassHalf, huntTimeLabel, MapRunText.HuntTime(live), huntTimeColor, null, left, valueX, right, y, rowHeight);
            y += rowHeight;
        }

        var hint = tier.TunedForFullParty ? Loc.T(L.Spot.FullPartyHint) : null;
        DrawFact(FontAwesomeIcon.Users, partyLabel, party, Styling.TextSecondary, hint, left, valueX, right, y, rowHeight);
        y += rowHeight;
        if (tier.HasPortal)
        {
            DrawFact(FontAwesomeIcon.DoorOpen, portalLabel, Loc.T(L.Spot.PortalAvailable), Styling.TextSecondary, null, left, valueX, right, y, rowHeight);
            y += rowHeight;
        }

        var flyingColor = spot.FlyingUnlocked ? Styling.AccentMintSoft : Styling.AccentAmberSoft;
        DrawFact(FontAwesomeIcon.Feather, flyingLabel, flying, flyingColor, null, left, valueX, right, y, rowHeight);
        return y + rowHeight;
    }

    private static void DrawFact(FontAwesomeIcon icon, string label, string value, Vector4 valueColor, string? hint, float left, float valueX, float right, float top, float rowHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var midY = top + rowHeight * 0.5f;
        var iconSize = TextDraw.IconSize(icon);
        TextDraw.Icon(icon, new Vector2(left, midY - iconSize.Y * 0.5f), Styling.TextDim);

        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(left + FactIconColumn * scale, midY - labelSize.Y * 0.5f), Styling.TextMuted);

        var shown = TextDraw.Truncate(value, right - valueX);
        var valueSize = TextDraw.Measure(shown);
        var valueY = midY - valueSize.Y * 0.5f;
        TextDraw.At(shown, new Vector2(valueX, valueY), valueColor);
        if (hint is not null)
        {
            TextDraw.Trailing(hint, valueX + valueSize.X, right, valueY, Styling.AccentAmberSoft);
        }
    }

    private static void DrawButtons(in TreasureTier tier, in DigSpot spot, float right, float top, float buttonHeight, MapRefusal runBlocker, bool setupNeeded, bool runRunning)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var runLabel = Loc.T(L.MapRun.Run);
        var goToLabel = Loc.T(L.Spot.GoTo);
        var flagLabel = Loc.T(L.Spot.Flag);
        var runX = right - PillButton.Width(runLabel, FontAwesomeIcon.Play);
        var goToRight = runX - ButtonGap * scale;
        var goToX = goToRight - PillButton.Width(goToLabel, FontAwesomeIcon.LocationArrow);
        var flagX = goToX - ButtonGap * scale - PillButton.Width(flagLabel, FontAwesomeIcon.MapMarkerAlt);

        ImGui.SetCursorScreenPos(new Vector2(flagX, top));
        if (PillButton.Draw("##atm_spot_flag", flagLabel, Styling.AccentBlue, PillButton.Emphasis.Tinted, FontAwesomeIcon.MapMarkerAlt,
                height: Layout.ActionPillHeight, tooltip: Loc.T(L.Spot.FlagHint)))
        {
            SpotActions.PlaceFlag(tier, spot);
        }

        ImGui.SetCursorScreenPos(new Vector2(goToX, top));
        if (PillButton.Draw("##atm_spot_goto", goToLabel, Styling.AccentGlow, PillButton.Emphasis.Tinted, FontAwesomeIcon.LocationArrow,
                enabled: !setupNeeded, height: Layout.ActionPillHeight, tooltip: Loc.T(L.Spot.GoToHint)))
        {
            AutoGoto.StartToSpot(spot, runRunning);
        }

        // A disabled pill takes no hover, so the reason it is disabled is shown from its rectangle.
        if (setupNeeded && Hit.HoveringRect(new Vector2(goToX, top), new Vector2(goToRight, top + buttonHeight)))
        {
            Tooltip.Show(Loc.T(L.Goto.NeedsPlugins));
        }

        var canRun = runBlocker == MapRefusal.None;
        ImGui.SetCursorScreenPos(new Vector2(runX, top));
        if (PillButton.Draw("##atm_map_run", runLabel, Styling.AccentGlow, PillButton.Emphasis.Filled, FontAwesomeIcon.Play,
                enabled: canRun, height: Layout.ActionPillHeight, tooltip: Loc.T(L.MapRun.RunHint)))
        {
            AutoMapRun.Start(Plugin.Instance);
        }

        if (!canRun && Hit.HoveringRect(new Vector2(runX, top), new Vector2(right, top + buttonHeight)))
        {
            Tooltip.Show(Loc.T(MapOutcomes.Reason(runBlocker)));
        }
    }

    private static void Compose(in TreasureTier tier, in DigSpot spot)
    {
        var identity = new Identity(tier.Rank, spot.SpotIndex, spot.FlyingUnlocked);
        if (identity == cachedIdentity && ReferenceEquals(Loc.Current, cachedLanguage))
        {
            return;
        }

        cachedIdentity = identity;
        cachedLanguage = Loc.Current;
        var coordinates = Loc.T(L.Spot.Coordinates, MapCoordinates.Format(spot.MapCoordinates.X), MapCoordinates.Format(spot.MapCoordinates.Y));
        location = string.Concat(spot.ZoneName, TextDraw.Separator, coordinates);
        party = tier.MaxPartySize > 1 ? Loc.T(L.Spot.PartyGroup, tier.MaxPartySize) : Loc.T(L.Spot.PartySolo);
        flying = Loc.T(spot.FlyingUnlocked ? L.Spot.FlyingUnlocked : L.Spot.FlyingLocked);
    }
}
