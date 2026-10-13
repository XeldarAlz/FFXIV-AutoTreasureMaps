using AutoTreasureMaps.Core.Game.Ops;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace AutoTreasureMaps.Core.Market;

// Sequence counts the answers, so a caller notes it before acting and waits for it to move.
internal readonly record struct ListingsAnswer(int Sequence, byte Count, int Error);

internal readonly record struct PurchaseAnswer(int Sequence, uint ItemId, uint MessageId);

// The game's handlers for the board's answers, hooked only while a visit lasts. The listing count and waiting flag in
// the game's store mislead, and the purchase event Dalamud raises fires for refusals too, so the answers are taken
// where the game reads them. Each detour runs the game's code first and only copies what it was given; the lock covers
// a handler that runs off the framework thread.
internal sealed unsafe class MarketHooks : IDisposable
{
    private readonly object gate = new();
    private readonly Hook<InfoProxyItemSearch.Delegates.ProcessRequestResult> requestResultHook;
    private readonly Hook<InfoProxyItemSearch.Delegates.ProcessPurchaseResponse> purchaseResponseHook;
    private ListingsAnswer listingsAnswer;
    private PurchaseAnswer purchaseAnswer;

    private MarketHooks()
    {
        requestResultHook = Svc.Hook.HookFromAddress<InfoProxyItemSearch.Delegates.ProcessRequestResult>(
            InfoProxyItemSearch.Addresses.ProcessRequestResult.Value, OnRequestResult);
        try
        {
            purchaseResponseHook = Svc.Hook.HookFromAddress<InfoProxyItemSearch.Delegates.ProcessPurchaseResponse>(
                InfoProxyItemSearch.Addresses.ProcessPurchaseResponse.Value, OnPurchaseResponse);
        }
        catch
        {
            requestResultHook.Dispose();
            throw;
        }

        requestResultHook.Enable();
        purchaseResponseHook.Enable();
    }

    public ListingsAnswer Listings
    {
        get
        {
            lock (gate)
            {
                return listingsAnswer;
            }
        }
    }

    public PurchaseAnswer Purchase
    {
        get
        {
            lock (gate)
            {
                return purchaseAnswer;
            }
        }
    }

    // Null when this game version lacks one of the functions the visit needs, or a hook could not be placed.
    public static MarketHooks? TryCreate()
    {
        if (!MarketBoardOps.Supported())
        {
            RunLog.Warning("Market: a market board function was not found in this game version.");
            return null;
        }

        try
        {
            return new MarketHooks();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Market: hooking the market board's answers failed");
            return null;
        }
    }

    public void Dispose()
    {
        requestResultHook.Dispose();
        purchaseResponseHook.Dispose();
    }

    private void OnRequestResult(InfoProxyItemSearch* proxy, byte resultCount, int errorCode)
    {
        requestResultHook.Original(proxy, resultCount, errorCode);
        lock (gate)
        {
            listingsAnswer = new ListingsAnswer(listingsAnswer.Sequence + 1, resultCount, errorCode);
        }
    }

    private void OnPurchaseResponse(InfoProxyItemSearch* proxy, uint itemId, uint errorMessageId)
    {
        purchaseResponseHook.Original(proxy, itemId, errorMessageId);
        lock (gate)
        {
            purchaseAnswer = new PurchaseAnswer(purchaseAnswer.Sequence + 1, itemId, errorMessageId);
        }
    }
}
