using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace AutoTreasureMaps.Core.Treasure;

// The client keeps its one deciphered map in a fixed KeyItems slot and the spot subrow in the first word of the event
// item manager, and its own two getters read exactly those. The getters come first; the raw reads stand in when a game
// update leaves their signatures unresolved. A read is cached for the frame, so the window can ask every frame.
internal static unsafe class HeldMapReader
{
    private const int DecipheredMapSlot = 123;
    private const long NoFrame = -1;

    private static long cachedFrame = NoFrame;
    private static HeldMap? cached;

    public static bool NativeAvailable
        => EventItemManager.Addresses.Instance.Value != 0
        && EventItemManager.Addresses.GetTreasureHuntRank.Value != 0
        && EventItemManager.Addresses.GetTreasureSpotSubKey.Value != 0;

    public static bool TryRead(TreasureCatalog catalog, out HeldMap held)
    {
        var frame = CurrentFrame();
        if (frame == NoFrame || frame != cachedFrame)
        {
            cached = NativeAvailable ? ReadNative() : ReadKeyItemSlot(catalog);
            cachedFrame = frame;
        }

        held = cached.GetValueOrDefault();
        return cached.HasValue;
    }

    public static HeldMap? ReadNative()
    {
        if (!NativeAvailable || !Svc.ClientState.IsLoggedIn)
        {
            return null;
        }

        var manager = EventItemManager.Instance();
        if (manager is null)
        {
            return null;
        }

        var rank = manager->GetTreasureHuntRank();
        if (rank is 0 or > byte.MaxValue)
        {
            return null;
        }

        return new HeldMap((byte)rank, manager->GetTreasureSpotSubKey(), HeldMapSource.Native);
    }

    public static HeldMap? ReadKeyItemSlot(TreasureCatalog catalog)
    {
        if (!Svc.ClientState.IsLoggedIn || EventItemManager.Addresses.Instance.Value == 0)
        {
            return null;
        }

        var manager = EventItemManager.Instance();
        if (manager is null || !catalog.TryGetByKeyItem(KeyItemInSlot(), out var tier))
        {
            return null;
        }

        return new HeldMap(tier.Rank, *(ushort*)manager, HeldMapSource.KeyItemSlot);
    }

    public static uint KeyItemInSlot()
    {
        var inventory = InventoryManager.Instance();
        if (inventory is null)
        {
            return 0;
        }

        var container = inventory->GetInventoryContainer(InventoryType.KeyItems);
        if (container is null || !container->IsLoaded || container->Size <= DecipheredMapSlot)
        {
            return 0;
        }

        var slot = container->GetInventorySlot(DecipheredMapSlot);
        return slot is null ? 0 : slot->ItemId;
    }

    private static long CurrentFrame()
    {
        var framework = Framework.Instance();
        return framework is null ? NoFrame : framework->FrameCounter;
    }
}
