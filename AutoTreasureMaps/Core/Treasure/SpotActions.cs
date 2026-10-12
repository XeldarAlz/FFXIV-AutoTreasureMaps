using AutoTreasureMaps.Core.Game;
using AutoTreasureMaps.Core.Localization;
using AutoTreasureMaps.Core.Tasks;
using AutoTreasureMaps.Core.Travel;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoTreasureMaps.Core.Treasure;

internal static class SpotActions
{
    public static void PrintHeld(TreasureCatalog catalog)
    {
        var state = HeldSpot.Read(catalog, out var held, out var tier, out var spot);
        LogReadings(catalog);
        if (!Report(state, held))
        {
            return;
        }

        var x = MapCoordinates.Format(spot.MapCoordinates.X);
        var y = MapCoordinates.Format(spot.MapCoordinates.Y);
        RunLog.Info($"Spot: {tier.Name} (rank {held.Rank}, spot {held.SpotIndex}, read through {held.Source}) in {spot.ZoneName} (territory {spot.TerritoryId}, map {spot.MapId}), Level {spot.LevelId} at {spot.Position}, map X {x} Y {y}, flying {(spot.FlyingUnlocked ? "unlocked" : "locked")}.");
        LogStandingOffset(spot);
        Svc.Chat.Print($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatHeld, tier.Name, spot.ZoneName, x, y)}");
    }

    public static void Flag(TreasureCatalog catalog)
    {
        var state = HeldSpot.Read(catalog, out var held, out var tier, out var spot);
        if (!Report(state, held))
        {
            return;
        }

        if (!PlaceFlag(tier, spot))
        {
            Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatFlagFailed)}");
            return;
        }

        var x = MapCoordinates.Format(spot.MapCoordinates.X);
        var y = MapCoordinates.Format(spot.MapCoordinates.Y);
        Svc.Chat.Print($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatFlagged, spot.ZoneName, x, y)}");
    }

    public static bool PlaceFlag(in TreasureTier tier, in DigSpot spot)
    {
        var placed = MapFlag.PlaceAndOpen(spot.TerritoryId, spot.MapId, spot.Position, tier.Name);
        if (placed)
        {
            RunLog.Info($"Spot: flagged the dig spot in {spot.ZoneName} (territory {spot.TerritoryId}, map {spot.MapId}) at {spot.Position}.");
        }
        else
        {
            RunLog.Warning("Spot: the map agent is not available, so no flag was placed.");
        }

        return placed;
    }

    public static void GoTo(TreasureCatalog catalog, bool runRunning)
    {
        var state = HeldSpot.Read(catalog, out var held, out _, out var spot);
        if (!Report(state, held))
        {
            return;
        }

        AutoGoto.StartToSpot(spot, runRunning);
    }

    public static void ListTiers(TreasureCatalog catalog)
    {
        var tiers = catalog.Tiers;
        for (var index = 0; index < tiers.Length; index++)
        {
            var tier = tiers[index];
            RunLog.Info($"Tier {tier.Rank}: {tier.Name}, {tier.Expansion}, timeworn item {tier.TimewornItemId}, key item {tier.KeyItemId}, portal key {tier.PortalKeyItemId}, party {tier.MaxPartySize}, {tier.SpotCount} spots, {(tier.Tradable ? "tradable" : "untradable")}{(tier.Undersea ? ", undersea" : string.Empty)}.");
        }

        Svc.Chat.Print($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatTiers, tiers.Length)}");
    }

    // Both reads side by side, so a check in game shows whether the fallback agrees with the client's own getters.
    private static void LogReadings(TreasureCatalog catalog)
    {
        var native = Describe(HeldMapReader.ReadNative());
        var fallback = Describe(HeldMapReader.ReadKeyItemSlot(catalog));
        RunLog.Info($"Spot: getters {(HeldMapReader.NativeAvailable ? "resolved" : "unresolved")}, getter read {native}, slot 123 holds item {HeldMapReader.KeyItemInSlot()}, fallback read {fallback}.");
    }

    // Standing on a spawned coffer and asking again measures how far the coffer lies from the Level point.
    private static void LogStandingOffset(in DigSpot spot)
    {
        if (Svc.ClientState.TerritoryType != spot.TerritoryId || Svc.Objects.LocalPlayer is not { } player)
        {
            return;
        }

        var position = player.Position;
        RunLog.Info($"Spot: standing at {position}, {Vector3.Distance(position, spot.Position):F1}m from the Level point ({GroundDistance.Between(position, spot.Position):F1}m across, {position.Y - spot.Position.Y:F1}m above it).");
    }

    private static string Describe(HeldMap? held) => held is { } map ? $"rank {map.Rank} spot {map.SpotIndex}" : "none";

    private static bool Report(HeldSpotState state, in HeldMap held)
    {
        if (state == HeldSpotState.Resolved)
        {
            return true;
        }

        if (state == HeldSpotState.None)
        {
            Svc.Chat.Print($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatNone)}");
            return false;
        }

        RunLog.Warning($"Spot: rank {held.Rank} spot {held.SpotIndex} did not resolve to a known tier and Level point.");
        Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Spot.ChatUnresolved, held.Rank, held.SpotIndex)}");
        return false;
    }
}
