using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;

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

public class ExampleTask(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Example Task";
    public string Name => TaskName;
    public async Task RunAsync(CancellationToken ct)
    {
        await NavigationUtils.EnsureMainViewAsync(botApi, ct);

        var location = await botApi.WaitForTemplateAsync("example_template.png", ct);

        if (location is not null)
        {
            await botApi.TapAsync(location.Value, ct);
        }
    }
}