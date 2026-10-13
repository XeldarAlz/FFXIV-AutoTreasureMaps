using AutoTreasureMaps.Core.Game;
using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Game.Watchers;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Treasure;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Globalization;

namespace AutoTreasureMaps.Core.Market;

internal enum MarketBlocker : byte
{
    None,
    Off,
    Unsupported,
    FreeTrial,
    InDuty,
    Stopped,
    NoCaps,
    AllHeld,
    BudgetSpent,
    NotEnoughGil,
    BagFull,
    BoardLocked,
}

// Available is how many maps the money left could buy at the cheapest cap, as far as the check can tell.
internal readonly record struct MarketCheck(MarketBlocker Blocker, int Available)
{
    public bool CanBuy => Blocker == MarketBlocker.None;
}

// What may be bought and with how much gil. Session is the run's, or null outside a run, which then has the whole budget.
// The check is cheap enough for every frame: the bags are read once a frame, the rest are flag and gil reads.
internal static class MarketRules
{
    // Map ranks start at 1, so 0 names no tier.
    public const byte AnyTier = 0;

    public static int CapOf(Configuration configuration, byte rank)
        => configuration.MarketPriceCaps.TryGetValue(rank, out var cap) ? Math.Max(0, cap) : 0;

    public static void SetCap(Configuration configuration, byte rank, int cap)
    {
        if (cap <= 0)
        {
            if (configuration.MarketPriceCaps.Remove(rank))
            {
                configuration.SaveDebounced();
            }

            return;
        }

        if (configuration.MarketPriceCaps.TryGetValue(rank, out var current) && current == cap)
        {
            return;
        }

        configuration.MarketPriceCaps[rank] = cap;
        configuration.SaveDebounced();
    }

    // Undersea tiers stay locked off, so buying one would only fill a bag slot.
    public static bool CanBeBought(in TreasureTier tier) => tier.Tradable && !tier.Undersea;

    public static long BudgetLeft(Configuration configuration, TreasureSession? session)
        => Math.Max(0L, configuration.MarketRunBudget - (long)(session?.GilSpentOnMaps ?? 0));

    // The most one map may cost now: the budget left or the gil above the amount to keep, whichever is lower.
    public static long Spendable(Configuration configuration, TreasureSession? session)
        => Math.Min(BudgetLeft(configuration, session), Math.Max(0L, MarketBoardOps.Gil() - (long)configuration.MarketKeepGil));

    public static MarketCheck Check(Plugin plugin, TreasureSession? session, byte onlyRank = AnyTier)
    {
        var configuration = plugin.Configuration;
        var blocker = GateBlocker(configuration, session);
        if (blocker != MarketBlocker.None)
        {
            return new MarketCheck(blocker, 0);
        }

        var owned = MapBag.Read(plugin.Catalog);
        var tiers = plugin.Catalog.Tiers;
        var considered = 0;
        var cheapest = int.MaxValue;
        for (var index = 0; index < tiers.Length; index++)
        {
            var tier = tiers[index];
            if (!Considered(configuration, session, tier, onlyRank))
            {
                continue;
            }

            considered++;
            if (!Holds(owned, tier.Rank))
            {
                cheapest = Math.Min(cheapest, CapOf(configuration, tier.Rank));
            }
        }

        if (considered == 0)
        {
            return new MarketCheck(MarketBlocker.NoCaps, 0);
        }

        if (cheapest == int.MaxValue)
        {
            return new MarketCheck(MarketBlocker.AllHeld, 0);
        }

        var budgetLeft = BudgetLeft(configuration, session);
        if (budgetLeft < cheapest)
        {
            return new MarketCheck(MarketBlocker.BudgetSpent, 0);
        }

        var aboveKeep = MarketBoardOps.Gil() - (long)configuration.MarketKeepGil;
        if (aboveKeep < cheapest)
        {
            return new MarketCheck(MarketBlocker.NotEnoughGil, 0);
        }

        if (MarketBoardOps.EmptyBagSlots() == 0)
        {
            return new MarketCheck(MarketBlocker.BagFull, 0);
        }

        if (!MarketBoards.Reachable(MarketBoards.Resolve(configuration.MarketBoard)))
        {
            return new MarketCheck(MarketBlocker.BoardLocked, 0);
        }

        return new MarketCheck(MarketBlocker.None, (int)Math.Clamp(Math.Min(budgetLeft, aboveKeep) / cheapest, 1L, int.MaxValue));
    }

    // The tiers a visit tries, in the order the Maps settings choose.
    public static List<TreasureTier> Candidates(Plugin plugin, TreasureSession? session, byte onlyRank)
    {
        var configuration = plugin.Configuration;
        var owned = MapBag.Read(plugin.Catalog);
        var tiers = plugin.Catalog.Tiers;
        var highestFirst = configuration.MapOrder == MapOrder.HighestFirst;
        var candidates = new List<TreasureTier>();
        for (var step = 0; step < tiers.Length; step++)
        {
            var tier = tiers[highestFirst ? tiers.Length - 1 - step : step];
            if (Considered(configuration, session, tier, onlyRank) && !Holds(owned, tier.Rank))
            {
                candidates.Add(tier);
            }
        }

        return candidates;
    }

    // An item id, a tier's whole name, or a piece of one in the client's language. A whole name wins, and among pieces a
    // tradable tier wins over an untradable one.
    public static bool TryFindTier(TreasureCatalog catalog, string query, out TreasureTier found)
    {
        var fragment = GameText.Fold(query);
        if (uint.TryParse(fragment, NumberStyles.None, CultureInfo.InvariantCulture, out var itemId))
        {
            return catalog.TryGetByTimewornItem(itemId, out found);
        }

        found = default;
        var matched = false;
        var tiers = catalog.Tiers;
        for (var index = 0; index < tiers.Length; index++)
        {
            var tier = tiers[index];
            var name = GameText.Fold(tier.Name);
            if (string.Equals(name, fragment, StringComparison.OrdinalIgnoreCase))
            {
                found = tier;
                return true;
            }

            if (GameText.Contains(name, fragment) && (!matched || (CanBeBought(tier) && !CanBeBought(found))))
            {
                found = tier;
                matched = true;
            }
        }

        return matched;
    }

    public static LocString Reason(MarketBlocker blocker) => blocker switch
    {
        MarketBlocker.Off => L.Market.ReasonOff,
        MarketBlocker.Unsupported => L.Market.ReasonUnsupported,
        MarketBlocker.FreeTrial => L.Market.ReasonFreeTrial,
        MarketBlocker.InDuty => L.Market.ReasonInDuty,
        MarketBlocker.Stopped => L.Market.ReasonStopped,
        MarketBlocker.NoCaps => L.Market.ReasonNoCaps,
        MarketBlocker.AllHeld => L.Market.ReasonAllHeld,
        MarketBlocker.BudgetSpent => L.Market.ReasonBudgetSpent,
        MarketBlocker.NotEnoughGil => L.Market.ReasonNotEnoughGil,
        MarketBlocker.BagFull => L.Market.ReasonBagFull,
        _ => L.Market.ReasonBoardLocked,
    };

    private static MarketBlocker GateBlocker(Configuration configuration, TreasureSession? session)
    {
        if (!configuration.MarketBuyingEnabled)
        {
            return MarketBlocker.Off;
        }

        if (!MarketBoardOps.Supported())
        {
            return MarketBlocker.Unsupported;
        }

        if (Svc.Condition[ConditionFlag.OnFreeTrial])
        {
            return MarketBlocker.FreeTrial;
        }

        if (DutyWatcher.InContent())
        {
            return MarketBlocker.InDuty;
        }

        return session is { MarketStopped: true } ? MarketBlocker.Stopped : MarketBlocker.None;
    }

    // A run buys only enabled tiers; a purchase that names its tier is the player's own choice.
    private static bool Considered(Configuration configuration, TreasureSession? session, in TreasureTier tier, byte onlyRank)
    {
        if (!CanBeBought(tier) || CapOf(configuration, tier.Rank) == 0)
        {
            return false;
        }

        if (onlyRank != AnyTier)
        {
            return tier.Rank == onlyRank;
        }

        return MapTierRules.IsEnabled(configuration, tier) && session?.MarketSkips(tier.Rank) != true;
    }

    private static bool Holds(ReadOnlySpan<OwnedMap> owned, byte rank)
    {
        for (var index = 0; index < owned.Length; index++)
        {
            if (owned[index].Tier.Rank == rank)
            {
                return true;
            }
        }

        return false;
    }
}
