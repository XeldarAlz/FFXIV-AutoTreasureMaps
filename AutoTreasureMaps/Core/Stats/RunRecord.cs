namespace AutoTreasureMaps.Core.Stats;

[Serializable]
public sealed class RunRecord
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public double DurationSeconds { get; set; }

    public int MapsCompleted { get; set; }

    public string JobAbbreviation { get; set; } = "";

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    public double MapsPerHour => DurationSeconds > 0 ? MapsCompleted / (DurationSeconds / 3600.0) : 0;
}
