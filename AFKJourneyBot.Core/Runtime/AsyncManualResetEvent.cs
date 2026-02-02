namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// Async-friendly manual reset event used as a pause gate.
/// </summary>
public sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource _tcs = CreateTcs();

    /// <summary>
    /// Creates a new event in set or reset state.
    /// </summary>
    /// <param name="set">True to start in the signaled state.</param>
    public AsyncManualResetEvent(bool set = false)
    {
        if (set)
        {
            _tcs.TrySetResult();
        }
    }

    /// <summary>
    /// True if the gate is open (signaled).
    /// </summary>
    public bool IsSet => _tcs.Task.IsCompleted;

    /// <summary>
    /// Asynchronously waits until the gate is open.
    /// </summary>
    /// <param name="ct">Cancellation token to stop waiting.</param>
    public Task WaitAsync(CancellationToken ct)
    {
        var waitTask = _tcs.Task;
        return ct.CanBeCanceled ? waitTask.WaitAsync(ct) : waitTask;
    }

    /// <summary>
    /// Opens the gate and releases all awaiters.
    /// </summary>
    public void Set()
    {
        _tcs.TrySetResult();
    }

    /// <summary>
    /// Closes the gate so future awaiters will wait.
    /// </summary>
    public void Reset()
    {
        if (!_tcs.Task.IsCompleted)
        {
            return;
        }

        _tcs = CreateTcs();
    }

    private static TaskCompletionSource CreateTcs()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
