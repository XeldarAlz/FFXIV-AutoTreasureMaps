namespace AutoTreasureMaps;

public enum HumanAction : byte
{
    MoveOff,
    Teleport,
    Dig,
    OpenCoffer,
    Engage,
}

public readonly record struct DelayRange(float MinSeconds, float MaxSeconds);

public sealed partial class Configuration
{
    public bool ReactionDelaysEnabled { get; set; } = true;

    public float MoveOffDelayMinSeconds { get; set; } = 1.0f;

    public float MoveOffDelayMaxSeconds { get; set; } = 3.0f;

    public float TeleportDelayMinSeconds { get; set; } = 0.3f;

    public float TeleportDelayMaxSeconds { get; set; } = 1.5f;

    public float DigDelayMinSeconds { get; set; } = 0.5f;

    public float DigDelayMaxSeconds { get; set; } = 2.0f;

    public float OpenCofferDelayMinSeconds { get; set; } = 0.5f;

    public float OpenCofferDelayMaxSeconds { get; set; } = 2.0f;

    public float EngageDelayMinSeconds { get; set; } = 0.5f;

    public float EngageDelayMaxSeconds { get; set; } = 2.0f;

    public DelayRange DelayFor(HumanAction action) => action switch
    {
        HumanAction.MoveOff => new DelayRange(MoveOffDelayMinSeconds, MoveOffDelayMaxSeconds),
        HumanAction.Teleport => new DelayRange(TeleportDelayMinSeconds, TeleportDelayMaxSeconds),
        HumanAction.Dig => new DelayRange(DigDelayMinSeconds, DigDelayMaxSeconds),
        HumanAction.OpenCoffer => new DelayRange(OpenCofferDelayMinSeconds, OpenCofferDelayMaxSeconds),
        _ => new DelayRange(EngageDelayMinSeconds, EngageDelayMaxSeconds),
    };

    public void SetDelay(HumanAction action, DelayRange range)
    {
        switch (action)
        {
            case HumanAction.MoveOff:
                (MoveOffDelayMinSeconds, MoveOffDelayMaxSeconds) = range;
                break;
            case HumanAction.Teleport:
                (TeleportDelayMinSeconds, TeleportDelayMaxSeconds) = range;
                break;
            case HumanAction.Dig:
                (DigDelayMinSeconds, DigDelayMaxSeconds) = range;
                break;
            case HumanAction.OpenCoffer:
                (OpenCofferDelayMinSeconds, OpenCofferDelayMaxSeconds) = range;
                break;
            default:
                (EngageDelayMinSeconds, EngageDelayMaxSeconds) = range;
                break;
        }
    }
}
