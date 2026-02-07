using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public sealed class PushRoutine(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Push Routine";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        var seasonTask = new PushSeasonAfkStages(botApi);
        var afkTask = new PushAfkStages(botApi);
        var legendTrialTask = new LegendTrial(botApi);

        Log.Information("Starting routine: {RoutineName}", TaskName);

        while (!ct.IsCancellationRequested)
        {
            await legendTrialTask.RunAsync(ct);
            await seasonTask.RunAsync(ct);
            await afkTask.RunAsync(ct);
        }
    }
}
