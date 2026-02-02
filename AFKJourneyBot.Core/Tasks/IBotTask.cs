namespace AFKJourneyBot.Core.Tasks;

/// <summary>
/// Represents a single bot action that can be executed by the task manager.
/// </summary>
public interface IBotTask
{
    /// <summary>
    /// Display name for UI and logs.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the task logic until completion or cancellation.
    /// </summary>
    /// <param name="ct">Cancellation token to stop the task.</param>
    Task RunAsync(CancellationToken ct);
}
