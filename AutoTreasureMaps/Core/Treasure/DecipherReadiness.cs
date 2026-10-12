using AutoTreasureMaps.Core.Game;
using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Game.Watchers;
using AutoTreasureMaps.Core.Tasks;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Globalization;

namespace AutoTreasureMaps.Core.Treasure;

internal enum MapQueryResult : byte
{
    Found,
    NotInBags,
    Unknown,
}

// What stops a decipher before it starts, and which map it would take. The blocker check is cheap enough for every
// frame: the held map read is cached for the frame and the rest are flag and unlock reads.
internal static class DecipherReadiness
{
    private const int ExactNameScore = 2;
    private const int EnabledScore = 1;

    // A run is active from Start until it is cleared, paused or not; the controller's Running would also count a goto.
    public static DecipherIssue Blocker(Plugin plugin)
        => Blocker(plugin.Catalog, plugin.Controller.SessionSnapshot is not null);

    public static DecipherIssue Blocker(TreasureCatalog catalog, bool runActive)
    {
        if (!Svc.ClientState.IsLoggedIn || Svc.Objects.LocalPlayer is null)
        {
            return DecipherIssue.NotLoggedIn;
        }

        var currentTask = clib.Services.Svc.Automation.CurrentTask;
        if (currentTask is AutoDecipher)
        {
            return DecipherIssue.Deciphering;
        }

        if (runActive)
        {
            return DecipherIssue.RunActive;
        }

        if (currentTask is not null)
        {
            return DecipherIssue.TaskRunning;
        }

        if (HeldMapReader.TryRead(catalog, out _))
        {
            return DecipherIssue.MapHeld;
        }

        if (!DecipherOps.Unlocked())
        {
            return DecipherIssue.NotUnlocked;
        }

        if (DutyWatcher.InContent())
        {
            return DecipherIssue.InDuty;
        }

        if (Svc.Condition[ConditionFlag.Mounted])
        {
            return DecipherIssue.Mounted;
        }

        return NpcInteraction.PlayerReady() ? DecipherIssue.None : DecipherIssue.PlayerBusy;
    }

    public static DecipherIssue PickNext(TreasureCatalog catalog, Configuration configuration, out OwnedMap pick)
    {
        var owned = MapBag.Read(catalog);
        if (MapTierRules.TryPickNext(owned, configuration, out pick))
        {
            return DecipherIssue.None;
        }

        return owned.IsEmpty ? DecipherIssue.NoMaps : DecipherIssue.NoEnabledMaps;
    }

    // A query names a timeworn item id or a piece of a tier name in the client's language. It is the player's own
    // choice, so a switched-off tier still matches; among several matches an exact name wins, then an enabled tier,
    // then the order setting. A query that is one tier's whole name means only that tier, even when a longer name
    // contains it (in Japanese grade 1 sits inside grade 10).
    public static MapQueryResult Find(TreasureCatalog catalog, Configuration configuration, string query, out OwnedMap pick, out TreasureTier named)
    {
        var fragment = GameText.Fold(query);
        var isItemId = uint.TryParse(fragment, NumberStyles.None, CultureInfo.InvariantCulture, out var itemId);
        var tiers = catalog.Tiers;
        var exactOnly = NamesTierExactly(tiers, isItemId, itemId, fragment);
        var owned = MapBag.Read(catalog);
        var highestFirst = configuration.MapOrder == MapOrder.HighestFirst;
        var bestScore = -1;
        pick = default;
        for (var step = 0; step < owned.Length; step++)
        {
            var candidate = owned[highestFirst ? owned.Length - 1 - step : step];
            var score = Score(candidate.Tier, configuration, isItemId, itemId, fragment, exactOnly);
            if (score > bestScore)
            {
                bestScore = score;
                pick = candidate;
            }
        }

        if (bestScore >= 0)
        {
            named = pick.Tier;
            return MapQueryResult.Found;
        }

        for (var index = 0; index < tiers.Length; index++)
        {
            if (Score(tiers[index], configuration, isItemId, itemId, fragment, exactOnly) >= 0)
            {
                named = tiers[index];
                return MapQueryResult.NotInBags;
            }
        }

        named = default;
        return MapQueryResult.Unknown;
    }

    private static int Score(in TreasureTier tier, Configuration configuration, bool isItemId, uint itemId, string fragment, bool exactOnly)
    {
        var exact = IsExact(tier, isItemId, itemId, fragment);
        if (!exact && (exactOnly || isItemId || !GameText.Contains(GameText.Fold(tier.Name), fragment)))
        {
            return -1;
        }

        return (exact ? ExactNameScore : 0) + (MapTierRules.IsEnabled(configuration, tier) ? EnabledScore : 0);
    }

    private static bool NamesTierExactly(ReadOnlySpan<TreasureTier> tiers, bool isItemId, uint itemId, string fragment)
    {
        for (var index = 0; index < tiers.Length; index++)
        {
            if (IsExact(tiers[index], isItemId, itemId, fragment))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExact(in TreasureTier tier, bool isItemId, uint itemId, string fragment)
        => isItemId ? tier.TimewornItemId == itemId : string.Equals(GameText.Fold(tier.Name), fragment, StringComparison.OrdinalIgnoreCase);
}
