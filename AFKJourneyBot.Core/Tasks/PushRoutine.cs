using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public sealed class PushRoutine(IBotApi botApi, AppConfig config) : IBotTask
{
    public const string TaskName = "Push Routine";
    public string Name => TaskName;

    private readonly LegendTrial _legendTrialTask = new(botApi, config);
    private readonly PushSeasonAfkStages _seasonTask = new(botApi, config);
    private readonly PushAfkStages _afkTask = new(botApi, config);

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await _legendTrialTask.RunAsync(ct);
            await _seasonTask.RunAsync(ct);
            await _afkTask.RunAsync(ct);
        }
    }
}
