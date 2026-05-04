using AFKJourneyBot.Core.Tasks;
using Serilog;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// Runs a single bot task at a time and manages stop state.
/// </summary>
public sealed class TaskManager
{
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    /// <summary>
    /// Creates a task manager with the given bot API.
    /// </summary>
    public TaskManager(IBotApi api)
    {
        Api = api;
    }

    /// <summary>
    /// Bot API used by tasks.
    /// </summary>
    public IBotApi Api { get; }

    /// <summary>
    /// True while a task is running.
    /// </summary>
    public bool IsRunning => _runningTask is { IsCompleted: false };

    /// <summary>
    /// Raised when running state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Runs a task to completion, cancellation, or failure.
    /// </summary>
    /// <param name="task">Task instance to run.</param>
    /// <param name="taskName">Display name used for logs.</param>
    public async Task RunTaskAsync(IBotTask task, string taskName)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("A task is already running.");
        }

        _cts = new CancellationTokenSource();

        Log.Information("Starting task: {TaskName}", taskName);

        _runningTask = Task.Run(() => task.RunAsync(_cts.Token));
        OnStateChanged();
        try
        {
            await _runningTask;
            Log.Information("Task completed: {TaskName}", taskName);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Task canceled: {TaskName}", taskName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Task failed: {TaskName}", taskName);
            throw;
        }
        finally
        {
            _runningTask = null;
            _cts.Dispose();
            _cts = null;
            OnStateChanged();
        }
    }

    /// <summary>
    /// Requests cancellation of the active task.
    /// </summary>
    public void Stop()
    {
        if (_cts == null)
        {
            return;
        }

        Log.Information("Stop requested");
        _cts.Cancel();
        OnStateChanged();
    }

    /// <summary>
    /// Invokes the state changed event.
    /// </summary>
    private void OnStateChanged()
        => StateChanged?.Invoke(this, EventArgs.Empty);
}
