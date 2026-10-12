using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AutoTreasureMaps.Core.Treasure;

// Container and Slot point at the first stack of the tier found, the slot a direct decipher would name.
internal readonly record struct OwnedMap(TreasureTier Tier, int Count, InventoryType Container, ushort Slot);

// The timeworn maps in the four bags, one entry per tier in rank order. A scan is cached for the frame so the window
// can ask every frame, and it refills one buffer, so the span is only good until the next scan.
internal static unsafe class MapBag
{
    private const long NoFrame = -1;

    private static readonly InventoryType[] Bags = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4];

    private static long cachedFrame = NoFrame;
    private static OwnedMap[] owned = [];
    private static int ownedCount;
    private static int[] counts = [];
    private static InventoryType[] firstContainers = [];
    private static ushort[] firstSlots = [];

    public static ReadOnlySpan<OwnedMap> Read(TreasureCatalog catalog)
    {
        var frame = CurrentFrame();
        if (frame == NoFrame || frame != cachedFrame)
        {
            Scan(catalog);
            cachedFrame = frame;
        }

        return owned.AsSpan(0, ownedCount);
    }

    public static bool TryFind(TreasureCatalog catalog, byte rank, out OwnedMap map)
    {
        var maps = Read(catalog);
        for (var index = 0; index < maps.Length; index++)
        {
            if (maps[index].Tier.Rank == rank)
            {
                map = maps[index];
                return true;
            }
        }

        map = default;
        return false;
    }

    private static void Scan(TreasureCatalog catalog)
    {
        var tiers = catalog.Tiers;
        EnsureCapacity(tiers.Length);
        Array.Clear(counts);
        ownedCount = 0;

        var inventory = InventoryManager.Instance();
        if (inventory is null)
        {
            return;
        }

        for (var bagIndex = 0; bagIndex < Bags.Length; bagIndex++)
        {
            var container = inventory->GetInventoryContainer(Bags[bagIndex]);
            if (container is null || !container->IsLoaded)
            {
                continue;
            }

            for (var slotIndex = 0; slotIndex < container->Size; slotIndex++)
            {
                var slot = container->GetInventorySlot(slotIndex);
                if (slot is null || slot->ItemId == 0 || !catalog.TryGetTimewornIndex(slot->ItemId, out var tierIndex))
                {
                    continue;
                }

                if (counts[tierIndex] == 0)
                {
                    firstContainers[tierIndex] = Bags[bagIndex];
                    firstSlots[tierIndex] = (ushort)slotIndex;
                }

                counts[tierIndex] += slot->Quantity;
            }
        }

        for (var tierIndex = 0; tierIndex < tiers.Length; tierIndex++)
        {
            if (counts[tierIndex] > 0)
            {
                owned[ownedCount++] = new OwnedMap(tiers[tierIndex], counts[tierIndex], firstContainers[tierIndex], firstSlots[tierIndex]);
            }
        }
    }

    private static void EnsureCapacity(int tierCount)
    {
        if (counts.Length == tierCount)
        {
            return;
        }

        owned = new OwnedMap[tierCount];
        counts = new int[tierCount];
        firstContainers = new InventoryType[tierCount];
        firstSlots = new ushort[tierCount];
    }

    private static long CurrentFrame()
    {
        var framework = Framework.Instance();
        return framework is null ? NoFrame : framework->FrameCounter;
    }
}
