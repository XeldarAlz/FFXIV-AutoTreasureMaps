namespace AutoTreasureMaps.Core;

internal static class AtmConstants
{
    public const string PrimaryCommand = "/atm";
    public const string AliasCommand = "/treasuremaps";

    public const string LogPrefix = "[ATM]";

    public const int SaveThrottleMs = 500;

    internal static class ThrottleKeys
    {
        public const string Save = "AutoTreasureMaps.Save";
    }
}
