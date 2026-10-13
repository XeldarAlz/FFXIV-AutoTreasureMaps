using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Market;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Treasure;
using AutoTreasureMaps.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Globalization;
using System.Numerics;

namespace AutoTreasureMaps.Windows.Sections;

// The bag scan and the readiness check are cached for the frame, and every label comes from a cache, so drawing the card
// every frame allocates only when a new count or a new next map first shows up.
internal static class MapBagCard
{
    private const float PadX = 18f;
    private const float PadY = 14f;
    private const float RowHeight = 28f;
    private const float IconColumn = 26f;
    private const float BadgeGap = 10f;
    private const float LabelGap = 10f;
    private const float SectionGap = 12f;
    private const string ButtonId = "##atm_decipher_next";
    private const string BuyButtonId = "##atm_buy_map";
    private const string CountPrefix = "×";

    private static readonly Dictionary<int, string> countLabels = [];

    private static CachedText hint;

    public static void Draw(Plugin plugin)
    {
        var catalog = plugin.Catalog;
        var configuration = plugin.Configuration;
        var owned = MapBag.Read(catalog);
        var pickIssue = DecipherReadiness.PickNext(catalog, configuration, out var next);
        var blocker = DecipherReadiness.Blocker(plugin);
        var issue = blocker != DecipherIssue.None ? blocker : pickIssue;

        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = MathF.Max(RowHeight * scale, ImGui.GetTextLineHeight());
        var rowCount = Math.Max(owned.Length, 1);
        var buttonHeight = Layout.ActionPillHeight * scale;
        var sectionGap = SectionGap * scale;
        var padY = PadY * scale;
        var size = new Vector2(width, padY + rowHeight * rowCount + sectionGap * 2f + buttonHeight + padY);
        var end = origin + size;
        Paint.Glass(drawList, origin, end, Styling.PanelRounding * scale, Styling.AccentNebula, 0.04f);

        var left = origin.X + PadX * scale;
        var right = end.X - PadX * scale;
        var y = origin.Y + padY;
        if (owned.IsEmpty)
        {
            DrawEmptyRow(left, right, y, rowHeight);
        }

        for (var index = 0; index < owned.Length; index++)
        {
            DrawRow(drawList, configuration, owned[index], left, right, y + rowHeight * index, rowHeight);
        }

        y += rowHeight * rowCount + sectionGap;
        Paint.Hairline(drawList, new Vector2(left, y), new Vector2(right, y));
        DrawFooter(plugin, issue, pickIssue, next, left, right, y + sectionGap, buttonHeight);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawRow(ImDrawListPtr drawList, Configuration configuration, in OwnedMap map, float left, float right, float top, float rowHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var midY = top + rowHeight * 0.5f;
        var state = MapTierRules.StateOf(configuration, map.Tier);
        var enabled = state == MapTierState.Enabled;
        var badgeWidth = Badge.Draw(drawList, StateLabel(state), StateColor(state), right, midY);

        var count = CountLabel(map.Count);
        var countSize = TextDraw.Measure(count);
        var countX = right - badgeWidth - BadgeGap * scale - countSize.X;
        TextDraw.At(count, new Vector2(countX, midY - countSize.Y * 0.5f), Styling.TextSecondary);

        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Scroll);
        TextDraw.Icon(FontAwesomeIcon.Scroll, new Vector2(left, midY - iconSize.Y * 0.5f), enabled ? Styling.AccentGlowSoft : Styling.TextMuted);

        var nameX = left + IconColumn * scale;
        var name = TextDraw.Truncate(map.Tier.Name, countX - LabelGap * scale - nameX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(nameX, midY - nameSize.Y * 0.5f), enabled ? Styling.TextStrong : Styling.TextDim);
    }

    private static void DrawEmptyRow(float left, float right, float top, float rowHeight)
    {
        var text = TextDraw.Truncate(Loc.T(L.Decipher.NoMaps), right - left);
        var textSize = TextDraw.Measure(text);
        TextDraw.At(text, new Vector2(left, top + (rowHeight - textSize.Y) * 0.5f), Styling.TextMuted);
    }

    private static void DrawFooter(Plugin plugin, DecipherIssue issue, DecipherIssue pickIssue, in OwnedMap next, float left, float right, float top, float buttonHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var label = Loc.T(L.Maps.DecipherNext);
        var buttonX = right - PillButton.Width(label, FontAwesomeIcon.Magic);
        var midY = top + buttonHeight * 0.5f;
        var buying = plugin.Configuration.MarketBuyingEnabled;
        var buyLabel = Loc.T(L.Market.BuyMap);
        var buyX = buying ? buttonX - LabelGap * scale - PillButton.Width(buyLabel, FontAwesomeIcon.Coins) : buttonX;
        var textRight = buyX - LabelGap * scale;
        if (pickIssue == DecipherIssue.None)
        {
            var caption = Loc.T(L.Maps.Next);
            var captionSize = TextDraw.SmallCapsSize(caption);
            TextDraw.SmallCaps(caption, new Vector2(left, midY - captionSize.Y * 0.5f), Styling.TextMuted);
            var nameX = left + captionSize.X + LabelGap * scale;
            var name = TextDraw.Truncate(next.Tier.Name, textRight - nameX);
            var nameSize = TextDraw.Measure(name);
            TextDraw.At(name, new Vector2(nameX, midY - nameSize.Y * 0.5f), Styling.TextSecondary);
        }
        else if (pickIssue == DecipherIssue.NoEnabledMaps)
        {
            var reason = TextDraw.Truncate(Loc.T(DecipherIssues.Reason(pickIssue)), textRight - left);
            var reasonSize = TextDraw.Measure(reason);
            TextDraw.At(reason, new Vector2(left, midY - reasonSize.Y * 0.5f), Styling.AccentAmberSoft);
        }

        if (buying)
        {
            DrawBuyButton(plugin, buyLabel, buyX, buttonX - LabelGap * scale, top, buttonHeight);
        }

        var enabled = issue == DecipherIssue.None;
        ImGui.SetCursorScreenPos(new Vector2(buttonX, top));
        if (PillButton.Draw(ButtonId, label, Styling.AccentGlow, PillButton.Emphasis.Filled, FontAwesomeIcon.Magic,
                enabled, Layout.ActionPillHeight, enabled ? Hint(next.Tier.Rank) : null))
        {
            AutoDecipher.StartNext(plugin);
        }

        // A disabled pill takes no hover, so the reason it is disabled is shown from its rectangle.
        if (!enabled && Hit.HoveringRect(new Vector2(buttonX, top), new Vector2(right, top + buttonHeight)))
        {
            Tooltip.Show(Loc.T(DecipherIssues.Reason(issue)));
        }
    }

    private static void DrawBuyButton(Plugin plugin, string label, float left, float right, float top, float buttonHeight)
    {
        var refusal = AutoBuyMap.Refusal(plugin, MarketRules.AnyTier);
        var enabled = refusal.Key is null;
        ImGui.SetCursorScreenPos(new Vector2(left, top));
        if (PillButton.Draw(BuyButtonId, label, Styling.AccentAmber, PillButton.Emphasis.Tinted, FontAwesomeIcon.Coins,
                enabled, Layout.ActionPillHeight, enabled ? Loc.T(L.Market.BuyMapHint) : null))
        {
            AutoBuyMap.Start(plugin, MarketRules.AnyTier);
        }

        if (!enabled && Hit.HoveringRect(new Vector2(left, top), new Vector2(right, top + buttonHeight)))
        {
            Tooltip.Show(Loc.T(refusal));
        }
    }

    private static string Hint(byte rank)
        => hint.Get(rank, static key => Loc.T(L.Maps.DecipherNextHint, NameOf((uint)key)));

    private static string NameOf(uint rank)
        => Plugin.Instance.Catalog.TryGetByRank(rank, out var tier) ? tier.Name : string.Empty;

    private static string CountLabel(int count)
    {
        if (countLabels.TryGetValue(count, out var label))
        {
            return label;
        }

        label = string.Concat(CountPrefix, count.ToString(CultureInfo.InvariantCulture));
        countLabels[count] = label;
        return label;
    }

    private static string StateLabel(MapTierState state) => state switch
    {
        MapTierState.Enabled => Loc.T(L.Maps.StateEnabled),
        MapTierState.Disabled => Loc.T(L.Maps.StateDisabled),
        _ => Loc.T(L.Maps.StateLocked),
    };

    private static Vector4 StateColor(MapTierState state) => state switch
    {
        MapTierState.Enabled => Styling.AccentMint,
        MapTierState.Disabled => Styling.TextMuted,
        _ => Styling.AccentBlue,
    };
}
