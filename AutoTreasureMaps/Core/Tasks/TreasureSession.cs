using ECommons.DalamudServices;
using System.Threading;

namespace AutoTreasureMaps.Core.Tasks;

// One run of the controller: the clock, the job and the maps finished. It outlives a pause, since every resume builds
// the run task afresh.
public sealed class TreasureSession
{
    private long pausedMs;
    private long pauseStartedAtMs;
    private DateTime? endedAt;

    public TreasureSession()
    {
        JobAbbreviation = CurrentJobAbbreviation();
    }

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public string JobAbbreviation { get; private set; }

    public int MapsCompleted { get; private set; }

    public bool EndedWithFault { get; private set; }

    public bool CompletedByStopCondition;

    internal bool Recorded;
    internal bool AfterActionDispatched;

    public bool DidNothing => MapsCompleted == 0;

    public TimeSpan Elapsed => (endedAt ?? DateTime.UtcNow) - StartedAt - TimeSpan.FromMilliseconds(PausedTotalMs);

    private long PausedTotalMs
        => pausedMs + (pauseStartedAtMs == 0 ? 0 : Environment.TickCount64 - pauseStartedAtMs);

    internal void CreditMap() => MapsCompleted++;

    public void Sample()
    {
        if (JobAbbreviation.Length == 0)
        {
            JobAbbreviation = CurrentJobAbbreviation();
        }
    }

    // A Stop unwinds the run loop through the same catch as a genuine fault, and only a genuine fault may resume the run.
    public void RecordFault(Exception exception, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested || exception is OperationCanceledException)
        {
            return;
        }

        EndedWithFault = true;
        // clib's task runner writes the same exception to dalamud.log when the task unwinds.
        RunLog.Record(RunLogLevel.Error, exception, "The treasure task ended with an unexpected error");
    }

    internal void ClearFault() => EndedWithFault = false;

    public void BeginPause()
    {
        if (pauseStartedAtMs != 0)
        {
            return;
        }

        pauseStartedAtMs = Environment.TickCount64;
    }

    public void EndPause()
    {
        if (pauseStartedAtMs == 0)
        {
            return;
        }

        pausedMs += Environment.TickCount64 - pauseStartedAtMs;
        pauseStartedAtMs = 0;
    }

    // Freezes the clock, so a finished run left on screen during its after-run action stops counting.
    internal void End()
    {
        if (endedAt is not null)
        {
            return;
        }

        EndPause();
        endedAt = DateTime.UtcNow;
    }

    private static string CurrentJobAbbreviation()
        => Svc.Objects.LocalPlayer?.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? string.Empty;
}
