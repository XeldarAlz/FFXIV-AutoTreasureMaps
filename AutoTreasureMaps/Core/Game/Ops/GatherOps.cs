using AutoTreasureMaps.Core.Gather;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Collections.Immutable;
using System.Numerics;

namespace AutoTreasureMaps.Core.Game.Ops;

// A gathering node that was opened and left standing; a node gathered out vanishes, so only these need skipping.
internal readonly record struct GatherNodeKey(uint BaseId, Vector3 Position);

// The live gathering nodes and the game's Gathering window. Framework thread only.
internal static unsafe class GatherOps
{
    public const int NoSlot = -1;

    private const string GatheringAddon = "Gathering";
    private const int SlotCount = 8;
    private const int CloseValue = -1;
    // Item ids 2 to 19 are the shards, crystals and clusters, which go to the crystal pouch and need no bag slot.
    private const uint FirstCrystalItemId = 2;
    private const uint LastCrystalItemId = 19;
    // The window's values: slot n starts at 5 + 11n, bit 0 of its flags at +5 marks an item that needs more Perception,
    // and 2 at +10 marks a collectable, which opens the collectable window instead of gathering.
    private const int FirstSlotValue = 5;
    private const int ValuesPerSlot = 11;
    private const int SlotFlagsOffset = 5;
    private const uint NeedsPerceptionFlag = 1;
    private const int CollectableOffset = 10;
    private const uint CollectableValue = 2;

    public static IGameObject? FindLiveNode(in MapGatherArea area, List<GatherNodeKey> standing)
    {
        if (Svc.Objects.LocalPlayer is not { } player)
        {
            return null;
        }

        var objects = Svc.Objects;
        IGameObject? nearest = null;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < objects.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is null || candidate.ObjectKind != ObjectKind.GatheringPoint || !candidate.IsTargetable)
            {
                continue;
            }

            if (!MapGatherTable.Contains(area.PointIds, candidate.BaseId) || IsStanding(standing, candidate))
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

    public static IGameObject? FindById(ulong gameObjectId)
    {
        var objects = Svc.Objects;
        for (var index = 0; index < objects.Length; index++)
        {
            var candidate = objects[index];
            if (candidate is not null && candidate.GameObjectId == gameObjectId && candidate.IsTargetable)
            {
                return candidate;
            }
        }

        return null;
    }

    public static GatherNodeKey KeyOf(IGameObject node) => new(node.BaseId, node.Position);

    public static bool WindowOpen() => TryGetWindow(out _);

    public static bool Gathering() => Svc.Condition[ConditionFlag.Gathering];

    public static bool ActionUnderWay() => Svc.Condition[ConditionFlag.ExecutingGatheringAction];

    public static bool HasItems()
    {
        if (!TryGetWindow(out var addon))
        {
            return false;
        }

        var itemIds = addon->ItemIds;
        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (itemIds[slot] != 0)
            {
                return true;
            }
        }

        return false;
    }

    // The slot showing any of the given maps; the map takes whichever slot is free, so every slot is read.
    public static int FindSlot(ImmutableArray<uint> mapItemIds, out uint itemId)
    {
        itemId = 0;
        if (!TryGetWindow(out var addon))
        {
            return NoSlot;
        }

        var itemIds = addon->ItemIds;
        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (itemIds[slot] != 0 && MapGatherTable.Contains(mapItemIds, itemIds[slot]))
            {
                itemId = itemIds[slot];
                return slot;
            }
        }

        return NoSlot;
    }

    // An item to spend the node on: a crystal first, since it needs no bag slot, then, when bagItemsAllowed, any other
    // item that is not a map, a collectable or one the gatherer's Perception locks out. refusedSlots holds a bit for
    // each slot that would not gather.
    public static int PickFillerSlot(ImmutableArray<uint> mapItemIds, int refusedSlots, bool bagItemsAllowed)
    {
        if (!TryGetWindow(out var addon))
        {
            return NoSlot;
        }

        var itemIds = addon->ItemIds;
        var fallback = NoSlot;
        for (var slot = 0; slot < SlotCount; slot++)
        {
            var itemId = itemIds[slot];
            if (itemId == 0 || (refusedSlots & (1 << slot)) != 0 || MapGatherTable.Contains(mapItemIds, itemId) || NeedsPerception(addon, slot) || IsCollectable(addon, slot))
            {
                continue;
            }

            if (itemId is >= FirstCrystalItemId and <= LastCrystalItemId)
            {
                return slot;
            }

            if (bagItemsAllowed && fallback == NoSlot)
            {
                fallback = slot;
            }
        }

        return fallback;
    }

    public static bool GatherSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount || !TryGetWindow(out var addon))
        {
            return false;
        }

        RunLog.Debug($"Gathering: gathering slot {slot} (item {addon->ItemIds[slot]})");
        Callback.Fire(&addon->AtkUnitBase, true, slot, 0);
        return true;
    }

    public static bool Close()
    {
        if (!TryGetWindow(out var addon))
        {
            return false;
        }

        Callback.Fire(&addon->AtkUnitBase, true, CloseValue);
        return true;
    }

    public static bool HasFreeBagSlot() => FreeBagSlots() > 0;

    public static uint FreeBagSlots()
    {
        var inventory = InventoryManager.Instance();
        return inventory is null ? 0 : inventory->GetEmptySlotsInBag();
    }

    private static bool IsStanding(List<GatherNodeKey> standing, IGameObject candidate)
    {
        var baseId = candidate.BaseId;
        var position = candidate.Position;
        for (var index = 0; index < standing.Count; index++)
        {
            if (standing[index].BaseId == baseId && standing[index].Position == position)
            {
                return true;
            }
        }

        return false;
    }

    private static bool NeedsPerception(AddonGathering* addon, int slot) => (SlotValue(addon, slot, SlotFlagsOffset) & NeedsPerceptionFlag) != 0;

    private static bool IsCollectable(AddonGathering* addon, int slot) => SlotValue(addon, slot, CollectableOffset) == CollectableValue;

    private static uint SlotValue(AddonGathering* addon, int slot, int offset)
    {
        var valueIndex = FirstSlotValue + ValuesPerSlot * slot + offset;
        var unitBase = &addon->AtkUnitBase;
        return unitBase->AtkValues is null || valueIndex >= unitBase->AtkValuesCount ? 0 : unitBase->AtkValues[valueIndex].UInt;
    }

    private static bool TryGetWindow(out AddonGathering* addon)
        => GenericHelpers.TryGetAddonByName(GatheringAddon, out addon) && addon is not null && GenericHelpers.IsAddonReady(&addon->AtkUnitBase);
}
