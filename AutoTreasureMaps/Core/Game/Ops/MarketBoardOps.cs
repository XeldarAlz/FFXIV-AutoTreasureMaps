using AutoTreasureMaps.Core.Market;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AutoTreasureMaps.Core.Game.Ops;

// The market board's search window and the game's listing store behind it. Read and act only on the framework thread.
internal static unsafe class MarketBoardOps
{
    public const int MaxListings = 100;

    private const string SearchAddon = "ItemSearch";
    private const string ResultAddon = "ItemSearchResult";

    public static bool Supported()
        => InfoProxyItemSearch.Addresses.ProcessRequestResult.Value != 0
        && InfoProxyItemSearch.Addresses.ProcessPurchaseResponse.Value != 0
        && InfoProxyItemSearch.Addresses.SetLastPurchasedItem.Value != 0
        && InfoProxyItemSearch.Addresses.SendPurchaseRequestPacket.Value != 0;

    public static IGameObject? FindNearestBoard()
    {
        if (Svc.Objects.LocalPlayer is not { } player)
        {
            return null;
        }

        var objects = Svc.Objects;
        var boardIds = MarketBoards.ObjectIds.AsSpan();
        IGameObject? nearest = null;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < objects.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is null || !candidate.IsTargetable || !boardIds.Contains(candidate.BaseId))
            {
                continue;
            }

            var distance = Vector3.DistanceSquared(player.Position, candidate.Position);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    public static bool SearchOpen() => NpcInteraction.IsAddonReady(SearchAddon);

    // True while a board window is still open. Each call closes one, and the close helper throttles the repeats.
    public static bool CloseNextWindow()
    {
        if (NpcInteraction.IsAddonReady(ResultAddon))
        {
            NpcInteraction.Close(ResultAddon);
            return true;
        }

        if (NpcInteraction.IsAddonReady(SearchAddon))
        {
            NpcInteraction.Close(SearchAddon);
            return true;
        }

        return false;
    }

    // For a stop, which cannot wait out the close throttle.
    public static void CloseAll()
    {
        CloseNow(ResultAddon);
        CloseNow(SearchAddon);
    }

    public static uint Gil()
    {
        var inventory = InventoryManager.Instance();
        return inventory is null ? 0 : inventory->GetGil();
    }

    public static int EmptyBagSlots()
    {
        var inventory = InventoryManager.Instance();
        return inventory is null ? 0 : (int)inventory->GetEmptySlotsInBag();
    }

    public static uint SearchedItem()
    {
        var proxy = InfoProxyItemSearch.Instance();
        return proxy is null ? 0 : proxy->SearchItemId;
    }

    // The request the result window sends, made only while the board is open so the server sees the board in use. The
    // game appends each page of rows at EntryCount, which is cleared first so rows left from an earlier search of the
    // same item cannot pass for this one's.
    public static bool RequestListings(uint itemId)
    {
        var proxy = InfoProxyItemSearch.Instance();
        if (proxy is null)
        {
            return false;
        }

        proxy->EntryCount = 0;
        proxy->SearchItemId = itemId;
        return proxy->RequestData();
    }

    // Every promised row is in once the store holds that many and each is a live listing of the item. The waiting flag
    // is never cleared and the count is set before any row arrives, so neither tells this alone.
    public static bool ListingsComplete(uint itemId, int promised)
    {
        var proxy = InfoProxyItemSearch.Instance();
        if (proxy is null || proxy->SearchItemId != itemId)
        {
            return false;
        }

        var expected = Math.Min(promised, MaxListings);
        if (proxy->EntryCount < expected)
        {
            return false;
        }

        var rows = proxy->Listings;
        for (var index = 0; index < expected; index++)
        {
            ref readonly var row = ref rows[index];
            if (row.ItemId != itemId || row.ListingId == 0 || row.UnitPrice == 0)
            {
                return false;
            }
        }

        return true;
    }

    public static int ReadListings(uint itemId, int promised, Span<MarketListing> into)
    {
        var proxy = InfoProxyItemSearch.Instance();
        if (proxy is null)
        {
            return 0;
        }

        var expected = Math.Min(Math.Min(promised, MaxListings), into.Length);
        var rows = proxy->Listings;
        var count = 0;
        for (var index = 0; index < expected; index++)
        {
            ref readonly var row = ref rows[index];
            if (row.ItemId != itemId || row.ListingId == 0 || row.IsMannequin)
            {
                continue;
            }

            into[count++] = new MarketListing(row.ListingId, row.RetainerId, row.UnitPrice, row.TotalTax, row.Quantity);
        }

        return count;
    }

    public static bool StillListed(in MarketListing listing, uint itemId)
    {
        var proxy = InfoProxyItemSearch.Instance();
        return proxy is not null && TryFindRow(proxy, listing, itemId, out _);
    }

    // Sent only for a row still in the store exactly as it was read, since the game sends the price it expects to pay.
    public static bool Purchase(in MarketListing listing, uint itemId)
    {
        var proxy = InfoProxyItemSearch.Instance();
        if (proxy is null || !TryFindRow(proxy, listing, itemId, out var row))
        {
            return false;
        }

        return proxy->SetLastPurchasedItem(row) && proxy->SendPurchaseRequestPacket();
    }

    // Either list can be empty until the game has loaded it, so a listing of one's own retainer may still come back as
    // the game's refusal.
    public static bool IsOwnRetainer(ulong retainerId)
    {
        if (retainerId == 0)
        {
            return false;
        }

        var manager = RetainerManager.Instance();
        if (manager is not null)
        {
            var retainers = manager->Retainers;
            for (var index = 0; index < retainers.Length; index++)
            {
                if (retainers[index].RetainerId == retainerId)
                {
                    return true;
                }
            }
        }

        var proxy = InfoProxyItemSearch.Instance();
        if (proxy is null)
        {
            return false;
        }

        var known = proxy->PlayerRetainers;
        var knownCount = Math.Min((int)proxy->PlayerRetainerCount, known.Length);
        for (var index = 0; index < knownCount; index++)
        {
            if (known[index].RetainerId == retainerId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindRow(InfoProxyItemSearch* proxy, in MarketListing listing, uint itemId, out MarketBoardListing* found)
    {
        found = null;
        var rows = proxy->Listings;
        var count = (int)Math.Min(proxy->EntryCount, (uint)MaxListings);
        for (var index = 0; index < count; index++)
        {
            ref var row = ref rows[index];
            if (row.ListingId != listing.ListingId)
            {
                continue;
            }

            if (row.ItemId != itemId || row.RetainerId != listing.RetainerId || row.UnitPrice != listing.UnitPrice
                || row.TotalTax != listing.TotalTax || row.Quantity != listing.Quantity)
            {
                return false;
            }

            found = (MarketBoardListing*)Unsafe.AsPointer(ref row);
            return true;
        }

        return false;
    }

    private static void CloseNow(string addonName)
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out var addon) && addon->IsVisible)
        {
            addon->Close(true);
        }
    }
}
