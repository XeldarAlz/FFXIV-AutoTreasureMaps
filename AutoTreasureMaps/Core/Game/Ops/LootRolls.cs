using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoTreasureMaps.Core.Game.Ops;

// The game's loot list keeps up to 16 items waiting for a roll. The Need or Greed window's buttons end in the client's
// own roll request, called with the loot list, the roll and the item's index; the request is found through its one
// call site, the window's roll handler, which passes Need as 1, Greed as 2 and Pass as 5. Read and called only on the
// framework thread.
internal static unsafe class LootRolls
{
    private const string RollRequestCallSite = "E8 ?? ?? ?? ?? EB 4A 48 8D 4F 10";

    private static bool resolved;
    private static delegate* unmanaged<Loot*, uint, uint, void> rollRequest;

    public static bool Available => Resolve() && LootList() is not null;

    // Items still waiting on this character's roll, from the chests the filter accepts.
    public static int Pending(Func<uint, bool> fromChest)
    {
        var loot = LootList();
        if (loot is null)
        {
            return 0;
        }

        var items = loot->Items;
        var pending = 0;
        for (var index = 0; index < items.Length; index++)
        {
            if (AwaitsRoll(items[index]) && fromChest(items[index].ChestObjectId))
            {
                pending++;
            }
        }

        return pending;
    }

    // The chests that already have items waiting, so a later roll leaves another chest's items alone.
    public static int PendingChests(Span<uint> into)
    {
        var loot = LootList();
        if (loot is null)
        {
            return 0;
        }

        var items = loot->Items;
        var count = 0;
        for (var index = 0; index < items.Length && count < into.Length; index++)
        {
            var chest = items[index].ChestObjectId;
            if (AwaitsRoll(items[index]) && !into[..count].Contains(chest))
            {
                into[count++] = chest;
            }
        }

        return count;
    }

    // Sends one roll per waiting item from the accepted chests and answers how many went out. A Need the item does not
    // allow falls back to Greed, and a Greed to Pass, as the window's own buttons would.
    public static int Roll(LootRoll choice, Func<uint, bool> fromChest)
    {
        if (choice == LootRoll.Manual || !Available)
        {
            return 0;
        }

        var loot = LootList();
        if (loot is null)
        {
            return 0;
        }

        var items = loot->Items;
        var sent = 0;
        for (var index = 0; index < items.Length; index++)
        {
            ref var item = ref items[index];
            if (!AwaitsRoll(item) || !fromChest(item.ChestObjectId))
            {
                continue;
            }

            var roll = RollFor(choice, item.RollState);
            RunLog.Info($"Loot: rolling {roll} on item {item.ItemId} x{item.ItemCount} (slot {index}, chest {item.ChestObjectId:X}, state {item.RollState}, {item.Time:F0}s of {item.MaxTime:F0}s left).");
            rollRequest(loot, (uint)roll, (uint)index);
            sent++;
        }

        return sent;
    }

    public static string Describe()
    {
        var loot = LootList();
        if (loot is null)
        {
            return "the loot list is not available";
        }

        var items = loot->Items;
        var builder = new System.Text.StringBuilder();
        for (var index = 0; index < items.Length; index++)
        {
            ref var item = ref items[index];
            if (item.ItemId == 0)
            {
                continue;
            }

            builder.Append(builder.Length == 0 ? string.Empty : ", ")
                .Append($"slot {index}: item {item.ItemId} x{item.ItemCount} from chest {item.ChestObjectId:X} ({item.RollState}, {item.RollResult}, mode {item.LootMode})");
        }

        return builder.Length == 0 ? "no items" : builder.ToString();
    }

    private static Loot* LootList() => Loot.Addresses.Instance.Value == 0 ? null : Loot.Instance();

    private static bool AwaitsRoll(in LootItem item)
        => item.ItemId != 0
        && item.RollResult == RollResult.UnAwarded
        && item.RollState is RollState.UpToNeed or RollState.UpToGreed or RollState.UpToPass;

    private static RollResult RollFor(LootRoll choice, RollState state) => (choice, state) switch
    {
        (_, RollState.UpToPass) => RollResult.Passed,
        (LootRoll.Pass, _) => RollResult.Passed,
        (LootRoll.Need, RollState.UpToNeed) => RollResult.Needed,
        _ => RollResult.Greeded,
    };

    private static bool Resolve()
    {
        if (resolved)
        {
            return rollRequest is not null;
        }

        resolved = true;
        if (!Svc.SigScanner.TryScanText(RollRequestCallSite, out var address))
        {
            RunLog.Warning("Loot: the client's roll request was not found, so loot rolls are left to you.");
            return false;
        }

        rollRequest = (delegate* unmanaged<Loot*, uint, uint, void>)address;
        RunLog.Debug($"Loot: roll request resolved at 0x{address:X}.");
        return true;
    }
}
