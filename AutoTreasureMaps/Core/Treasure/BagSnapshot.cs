using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Immutable;

namespace AutoTreasureMaps.Core.Treasure;

// Item counts across the bags, the crystal pouch and the currency pouch, taken before a coffer opens and again once its
// loot has settled; the difference is what the coffer gave. Gil is the currency pouch's item 1. A container that is not
// loaded reads as empty, which only happens around a zone change. Read only on the framework thread.
internal static unsafe class BagSnapshot
{
    private const uint GilItemId = 1;

    private static readonly InventoryType[] Containers =
    [
        InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4,
        InventoryType.Crystals, InventoryType.Currency,
    ];

    public static Dictionary<uint, int> Capture()
    {
        var counts = new Dictionary<uint, int>();
        var inventory = InventoryManager.Instance();
        if (inventory is null)
        {
            return counts;
        }

        for (var containerIndex = 0; containerIndex < Containers.Length; containerIndex++)
        {
            var container = inventory->GetInventoryContainer(Containers[containerIndex]);
            if (container is null || !container->IsLoaded)
            {
                continue;
            }

            for (var slotIndex = 0; slotIndex < container->Size; slotIndex++)
            {
                var slot = container->GetInventorySlot(slotIndex);
                if (slot is null || slot->ItemId == 0)
                {
                    continue;
                }

                counts[slot->ItemId] = counts.GetValueOrDefault(slot->ItemId) + slot->Quantity;
            }
        }

        return counts;
    }

    // Order-independent, so polling it to see whether the loot has stopped arriving allocates nothing.
    public static long Fingerprint()
    {
        var inventory = InventoryManager.Instance();
        if (inventory is null)
        {
            return 0;
        }

        long fingerprint = 0;
        for (var containerIndex = 0; containerIndex < Containers.Length; containerIndex++)
        {
            var container = inventory->GetInventoryContainer(Containers[containerIndex]);
            if (container is null || !container->IsLoaded)
            {
                continue;
            }

            for (var slotIndex = 0; slotIndex < container->Size; slotIndex++)
            {
                var slot = container->GetInventorySlot(slotIndex);
                if (slot is not null && slot->ItemId != 0)
                {
                    fingerprint += (slot->ItemId * 2_654_435_761L) ^ slot->Quantity;
                }
            }
        }

        return fingerprint;
    }

    // Only gains count: an item spent meanwhile, a potion in the fight, is not the coffer's doing.
    public static (int Gil, ImmutableArray<ItemGain> Items) Gains(Dictionary<uint, int> before, Dictionary<uint, int> after)
    {
        var gil = 0;
        var items = ImmutableArray.CreateBuilder<ItemGain>();
        foreach (var (itemId, count) in after)
        {
            var gained = count - before.GetValueOrDefault(itemId);
            if (gained <= 0)
            {
                continue;
            }

            if (itemId == GilItemId)
            {
                gil = gained;
                continue;
            }

            items.Add(new ItemGain(itemId, gained));
        }

        return (gil, items.ToImmutable());
    }
}
