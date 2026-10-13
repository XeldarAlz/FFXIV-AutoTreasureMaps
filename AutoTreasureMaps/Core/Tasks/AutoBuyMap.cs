using AutoTreasureMaps.Core.External;
using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Market;
using AutoTreasureMaps.Core.Supply;
using AutoTreasureMaps.Core.Treasure;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoTreasureMaps.Core.Tasks;

// Buys one map outside a run, from /atm buy or the Treasure page's button, under the same caps, budget and gil to keep
// as a run. The map stays in the bags.
internal sealed class AutoBuyMap(byte rank) : AutoCommon
{
    public static void HandleCommand(string query, Plugin plugin)
    {
        if (query.Length == 0)
        {
            Start(plugin, MarketRules.AnyTier);
            return;
        }

        if (!MarketRules.TryFindTier(plugin.Catalog, query, out var tier))
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Decipher.ChatUnknownTier, query)}");
            return;
        }

        if (!MarketRules.CanBeBought(tier))
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatNotTradable, tier.Name)}");
            return;
        }

        if (MarketRules.CapOf(plugin.Configuration, tier.Rank) == 0)
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatNoCap, tier.Name)}");
            return;
        }

        if (MapBag.TryFind(plugin.Catalog, tier.Rank, out _))
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatInBags, tier.Name)}");
            return;
        }

        Start(plugin, tier.Rank);
    }

    public static void Start(Plugin plugin, byte rank)
    {
        var refusal = Refusal(plugin, rank);
        if (refusal.Key is not null)
        {
            RunLog.Info($"Market buy refused: {refusal.Key}.");
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatRefused, Loc.T(refusal))}");
            return;
        }

        Svc.Chat.Print($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatStarted)}");
        clib.Services.Svc.Automation.Start(new AutoBuyMap(rank));
    }

    // Default when nothing stops a purchase now. Cheap enough for every frame.
    public static LocString Refusal(Plugin plugin, byte rank)
    {
        if (!Svc.ClientState.IsLoggedIn || Svc.Objects.LocalPlayer is null)
        {
            return L.Decipher.NotLoggedIn;
        }

        if (plugin.Controller.SessionSnapshot is not null)
        {
            return L.Decipher.RunActive;
        }

        if (clib.Services.Svc.Automation.CurrentTask is not null)
        {
            return L.Decipher.TaskRunning;
        }

        // The trip to the board needs the movement helpers, and its teleport costs gil before a missing one shows.
        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return L.Treasure.DetailSetupNeeded;
        }

        // A map bought without Decipher unlocked could never be run.
        if (!DecipherOps.Unlocked())
        {
            return L.Decipher.NotUnlocked;
        }

        var check = MarketRules.Check(plugin, null, rank);
        return check.CanBuy ? default : MarketRules.Reason(check.Blocker);
    }

    protected override async Task Execute()
    {
        var task = new MarketBuyTask(null, rank);
        var finished = false;
        await RunWithStatusFrom(task, async () => finished = await RunCancellable(task, MarketSupplyStep.TaskTimeoutMs, "market-buy"));
        if (task.Fault is { } fault)
        {
            RunLog.Record(RunLogLevel.Error, fault, "Buying a map ended with an unexpected error");
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatFailed, Loc.T(L.Market.FailUnexpected))}");
            return;
        }

        if (CancelToken.IsCancellationRequested)
        {
            Diag("Market: the purchase was stopped.");
            return;
        }

        if (!finished)
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatFailed, Loc.T(L.Market.FailTimedOut))}");
            return;
        }

        var answer = task.Answer;
        if (answer.Outcome == MapSupplyOutcome.Empty)
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Market.ChatNothingBought, Loc.T(answer.Reason))}");
        }
    }
}
