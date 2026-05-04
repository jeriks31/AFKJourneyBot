using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class DebugTask(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Debug";
    public const string TaskDescription = "Runs a lightweight development task used to verify runtime wiring and logging.";

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Information("Hello world!");
    }
}
