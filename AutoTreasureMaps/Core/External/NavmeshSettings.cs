using AutoTreasureMaps.Core.Localization;
using ECommons.DalamudServices;
using System.IO;
using System.Text.Json;

namespace AutoTreasureMaps.Core.External;

// The pathfinder exposes none of its settings over IPC, so the one that breaks automation is read from its saved file.
internal static class NavmeshSettings
{
    private const string ConfigFileName = "vnavmesh.json";
    private const string PayloadProperty = "Payload";
    private const string CancelOnInputProperty = "CancelMoveOnUserInput";

    private static bool warned;

    // With the setting on, any movement key or stick input drops the path this plugin queued, so the run stalls.
    public static void WarnIfInputCancelsMovement()
    {
        if (warned || !InputCancelsMovement())
        {
            return;
        }

        warned = true;
        RunLog.Warning("The pathfinder cancels its path on movement input; warned the player.");
        Svc.Chat.PrintError($"{AtmConstants.LogPrefix} {Loc.T(L.Run.ChatNavmeshInput)}");
    }

    private static bool InputCancelsMovement()
    {
        var directory = Svc.PluginInterface.ConfigFile.DirectoryName;
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        var path = Path.Combine(directory, ConfigFileName);
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty(PayloadProperty, out var payload)
                && payload.TryGetProperty(CancelOnInputProperty, out var setting)
                && setting.ValueKind == JsonValueKind.True;
        }
        catch (Exception exception)
        {
            RunLog.Debug($"Could not read the pathfinder's settings: {exception.Message}");
            return false;
        }
    }
}
