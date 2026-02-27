using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class DebugTask(IBotApi botApi) : IBotTask
{
    public static readonly string TaskName = "Debug";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Information("Hello world!");
    }
}
