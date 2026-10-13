using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTreasureMaps.Core.Game.Ops;

internal enum GearsetSwitchResult : byte
{
    AlreadyOnClass,
    Switched,
    NoGearset,
    InCombat,
    Refused,
    TimedOut,
    Cancelled,
}

// Gearsets are found by their class, never by a fixed number, since players reorder them. Indexes here are the API's
// 0-based ones; the game's list shows them plus one. Only the members that touch pointers are unsafe, because an async
// method cannot await inside an unsafe context.
internal static class GearsetSwitcher
{
    public const int NoGearset = -1;

    // The in-game gearset list holds 100 sets.
    private const int MaxGearsetCount = 100;
    // The class change is one server round trip; this leaves room for a slow one.
    private const int SwitchTimeoutMs = 10_000;
    private const int SwitchPollFrames = 5;
    // 0 keeps the glamour plate linked to the gearset.
    private const byte LinkedGlamourPlate = 0;
    // Crafters and gatherers are the ClassJob rows 8 to 18; every other class can fight.
    private const uint FirstCrafterClassJobId = 8;
    private const uint LastGathererClassJobId = 18;

    public static uint CurrentClassJobId() => Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0;

    public static bool OnCombatClass() => CurrentClassJobId() is not (0 or (>= FirstCrafterClassJobId and <= LastGathererClassJobId));

    public static unsafe int CurrentGearsetIndex()
    {
        var module = RaptureGearsetModule.Instance();
        return module == null ? NoGearset : module->CurrentGearsetIndex;
    }

    public static int ClassLevel(uint classJobId)
        => Svc.Data.GetExcelSheet<ClassJob>().TryGetRow(classJobId, out var classJob) ? Svc.PlayerState.GetClassJobLevel(classJob) : 0;

    // Among the gearsets of the class, the highest item level wins; a set missing its main hand cannot work the class.
    public static unsafe int FindBestGearset(uint classJobId)
    {
        var module = RaptureGearsetModule.Instance();
        if (module == null || classJobId == 0)
        {
            return NoGearset;
        }

        var best = NoGearset;
        var bestItemLevel = -1;
        for (var gearsetIndex = 0; gearsetIndex < MaxGearsetCount; gearsetIndex++)
        {
            if (!module->IsValidGearset(gearsetIndex))
            {
                continue;
            }

            var gearset = module->GetGearset(gearsetIndex);
            if (gearset == null || gearset->ClassJob != classJobId || gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.MainHandMissing))
            {
                continue;
            }

            if (gearset->ItemLevel <= bestItemLevel)
            {
                continue;
            }

            best = gearsetIndex;
            bestItemLevel = gearset->ItemLevel;
        }

        return best;
    }

    // Must run on the framework thread, as the run's tasks do. The preferred gearset is used while it still holds the
    // class; otherwise the best gearset of the class is.
    public static async Task<GearsetSwitchResult> EquipFor(uint classJobId, int preferredIndex, CancellationToken cancelToken)
    {
        if (CurrentClassJobId() == classJobId)
        {
            return GearsetSwitchResult.AlreadyOnClass;
        }

        var gearsetIndex = PickGearset(classJobId, preferredIndex);
        if (gearsetIndex == NoGearset)
        {
            Log($"Gearset: no gearset holds {ClassJobLabel(classJobId)}");
            return GearsetSwitchResult.NoGearset;
        }

        if (Svc.Condition[ConditionFlag.InCombat])
        {
            Log($"Gearset: in combat, so gearset {gearsetIndex + 1} for {ClassJobLabel(classJobId)} waits");
            return GearsetSwitchResult.InCombat;
        }

        if (!TryEquip(gearsetIndex))
        {
            return GearsetSwitchResult.Refused;
        }

        var deadline = Environment.TickCount64 + SwitchTimeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (cancelToken.IsCancellationRequested)
            {
                return GearsetSwitchResult.Cancelled;
            }

            if (CurrentClassJobId() == classJobId)
            {
                Log($"Gearset: now on {ClassJobLabel(classJobId)}");
                return GearsetSwitchResult.Switched;
            }

            try
            {
                await Svc.Framework.DelayTicks(SwitchPollFrames, cancelToken);
            }
            catch (OperationCanceledException)
            {
                return GearsetSwitchResult.Cancelled;
            }
        }

        Log($"Gearset: the class did not change within {SwitchTimeoutMs / TimeUnits.MillisecondsPerSecond}s of equipping gearset {gearsetIndex + 1} (still {ClassJobLabel(CurrentClassJobId())})");
        return GearsetSwitchResult.TimedOut;
    }

    // For a task that is being torn down and can no longer wait: the request goes out, and the class follows a moment later.
    public static bool EquipWithoutWaiting(uint classJobId, int preferredIndex)
    {
        if (CurrentClassJobId() == classJobId)
        {
            return true;
        }

        var gearsetIndex = PickGearset(classJobId, preferredIndex);
        return gearsetIndex != NoGearset && !Svc.Condition[ConditionFlag.InCombat] && TryEquip(gearsetIndex);
    }

    public static bool HasGearsetFor(uint classJobId, int preferredIndex) => PickGearset(classJobId, preferredIndex) != NoGearset;

    public static string ClassJobLabel(uint classJobId)
    {
        var abbreviation = Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault(classJobId)?.Abbreviation.ExtractText();
        return string.IsNullOrEmpty(abbreviation) ? $"class {classJobId}" : abbreviation;
    }

    private static unsafe int PickGearset(uint classJobId, int preferredIndex)
    {
        var module = RaptureGearsetModule.Instance();
        if (module != null && preferredIndex != NoGearset && module->IsValidGearset(preferredIndex))
        {
            var preferred = module->GetGearset(preferredIndex);
            if (preferred != null && preferred->ClassJob == classJobId)
            {
                return preferredIndex;
            }
        }

        return FindBestGearset(classJobId);
    }

    private static unsafe bool TryEquip(int gearsetIndex)
    {
        var module = RaptureGearsetModule.Instance();
        var gearset = module == null ? null : module->GetGearset(gearsetIndex);
        if (gearset == null)
        {
            return false;
        }

        Log($"Gearset: equipping gearset {gearsetIndex + 1} ({ClassJobLabel(gearset->ClassJob)}) from {ClassJobLabel(CurrentClassJobId())}");
        var result = module->EquipGearset(gearsetIndex, LinkedGlamourPlate);
        if (result == 0)
        {
            return true;
        }

        RunLog.Warning($"Gearset: the game refused gearset {gearsetIndex + 1} (EquipGearset returned {result})");
        return false;
    }

    private static void Log(string message) => RunLog.Info(message);
}
