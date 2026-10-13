using AutoTreasureMaps.Core.External;
using AutoTreasureMaps.Core.Game.Ops;
using AutoTreasureMaps.Core.Game.Watchers;
using ECommons.DalamudServices;

namespace AutoTreasureMaps.Core.Treasure;

// What stops the held map from being run. Cheap enough for every frame: the held map read is cached for the frame and
// the rest are flag, slot and unlock reads.
internal static class MapRunReadiness
{
    public static MapRefusal Blocker(Plugin plugin) => Blocker(plugin.Catalog, plugin.Controller.SessionSnapshot is not null, out _, out _);

    public static MapRefusal Blocker(TreasureCatalog catalog, bool runActive, out TreasureTier tier, out DigSpot spot)
    {
        tier = default;
        spot = default;
        if (!Svc.ClientState.IsLoggedIn || Svc.Objects.LocalPlayer is null)
        {
            return MapRefusal.NotLoggedIn;
        }

        if (runActive)
        {
            return MapRefusal.RunActive;
        }

        if (clib.Services.Svc.Automation.CurrentTask is not null)
        {
            return MapRefusal.TaskRunning;
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return MapRefusal.NeedsPlugins;
        }

        return MapBlocker(catalog, out tier, out spot);
    }

    // The map's own checks, also run by the task itself as it starts, where the run and the task are its own. A hunt
    // already under way counts as content to the game, so a duty only refuses while no hunt runs.
    public static MapRefusal MapBlocker(TreasureCatalog catalog, out TreasureTier tier, out DigSpot spot)
    {
        var state = HeldSpot.Read(catalog, out _, out tier, out spot);
        if (state == HeldSpotState.None)
        {
            return MapRefusal.NoMap;
        }

        if (state == HeldSpotState.Unresolved)
        {
            return MapRefusal.Unresolved;
        }

        if (tier.HasPortal && HeldMapReader.KeyItemInSlot() == tier.PortalKeyItemId)
        {
            return MapRefusal.PortalMap;
        }

        if (tier.Undersea)
        {
            return MapRefusal.Undersea;
        }

        if (!DigOps.Unlocked())
        {
            return MapRefusal.NotUnlocked;
        }

        return DutyWatcher.InContent() && !TreasureHuntDirector.IsActive() ? MapRefusal.InDuty : MapRefusal.None;
    }
}
