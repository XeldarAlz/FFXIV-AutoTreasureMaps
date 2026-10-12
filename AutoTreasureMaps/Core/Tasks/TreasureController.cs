using AutoTreasureMaps.Core.External;
using AutoTreasureMaps.Core.Ipc;
using clib.Services;

namespace AutoTreasureMaps.Core.Tasks;

internal sealed partial class TreasureController
{
    private const int SessionSampleIntervalMs = 1_000;

    private readonly TreasureProgress progress = new();

    private TreasureSession? session;
    private Func<TreasureSession, AutoCommon>? runTaskFactory;
    private AutoCommon? currentTask;
    private long nextSessionSampleAtMs;

    public bool Running => Svc.Automation.Running || Paused;

    public string Status => PauseReason switch
    {
        PauseReason.InContent => "Paused while you are in content",
        PauseReason.Manual    => "Paused",
        _                     => Svc.Automation.CurrentTask?.Status ?? "Idle",
    };

    public TreasurePhase Phase => progress.Phase;

    public TreasureProgress Progress => progress;

    public TreasureSession? SessionSnapshot => session;

    private static void Diag(string message)
        => RunLog.Info(message);

    // The run task lands with the treasure logic; until then there is nothing to begin.
    public void Start()
    {
        if (Running)
        {
            Diag("Start ignored: a run is already going.");
            return;
        }

        if (!RequiredPluginsReady())
        {
            return;
        }

        Diag("Start refused: No treasure run exists yet.");
    }

    public void Stop()
    {
        var ending = session;
        var wasPaused = Paused;
        currentTask = null;
        PauseReason = PauseReason.None;
        Svc.Automation.Stop();
        if (ending is not null)
        {
            ReleaseHelpers();
        }

        // Pause already credited the run, and anything done since was the player's own play.
        FinalizeRun(ending, sample: !wasPaused);
        ClearRun();
        if (ending is not null)
        {
            Diag("Stop requested; session cleared.");
        }
    }

    public void Tick()
    {
        if (session is null || session.Recorded || Paused)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (now < nextSessionSampleAtMs)
        {
            return;
        }

        nextSessionSampleAtMs = now + SessionSampleIntervalMs;
        session.Sample();
    }

    private static bool RequiredPluginsReady()
    {
        if (ExternalPlugins.AllRequiredInstalled())
        {
            return true;
        }

        var missing = ExternalPlugins.MissingRequiredNames();
        Diag($"Start aborted: required plugins missing ({missing}).");
        ECommons.DalamudServices.Svc.Chat.PrintError($"{AtmConstants.LogPrefix} Cannot start: install all required plugins first ({missing}).");
        return false;
    }

    // Every Resume and fault restart builds the run task afresh from the same factory.
    private void BeginRun(TreasureSession newSession, Func<TreasureSession, AutoCommon> taskFactory, string plan)
    {
        PauseReason = PauseReason.None;
        ResetFaultBudget();
        session = newSession;
        runTaskFactory = taskFactory;
        Diag($"Run starting: {plan}, job {newSession.JobAbbreviation}.");
        StartRun(newSession);
    }

    private void StartRun(TreasureSession owningSession)
    {
        progress.Reset();
        progress.SetPhase(TreasurePhase.Preparing);
        RunTask(runTaskFactory!(owningSession), () => OnRunEnded(owningSession));
    }

    private bool CanRestart() => runTaskFactory is not null;

    // The movement library fires OnCompleted off the game thread: its await of the task does not return to the framework
    // scheduler, and the runtime moves the continuation to the thread pool. Recording a run reads the object table, which
    // Dalamud allows only on the game thread, so the hand-off is moved back there.
    private void RunTask(AutoCommon task, Action onCompleted)
    {
        currentTask = task;
        Svc.Automation.Start(task, OnCompleted: () => _ = ECommons.DalamudServices.Svc.Framework.RunOnFrameworkThread(() => HandOff(task, onCompleted)));
    }

    private void HandOff(AutoCommon task, Action onCompleted)
    {
        if (!ReferenceEquals(currentTask, task))
        {
            Diag($"{task.GetType().Name} finished but is no longer the current task (stopped, paused, or superseded); skipping hand-off.");
            return;
        }

        currentTask = null;
        onCompleted();
    }

    private void ClearRun()
    {
        session = null;
        runTaskFactory = null;
        progress.Reset();
    }

    // A stopped task unwinds on a later frame, and after an unload that frame may never come, so the combat preset and
    // the pathfinder are released here as well.
    private static void ReleaseHelpers()
    {
        BossModIPC.Instance.ClearActive();
        BossModIPC.Instance.ReleaseMovement();
        NavmeshIPC.Instance.Stop();
    }
}

internal enum TreasurePhase { Idle, Preparing, Travelling, Fighting, Upkeep, Finishing, Paused }
