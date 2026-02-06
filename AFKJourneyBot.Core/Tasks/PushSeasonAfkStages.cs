using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class PushSeasonAfkStages(IBotApi botApi) : IBotTask
{
    private const int AttemptsPerFormation = 1; // TODO: Make configurable
    private const int FormationsToTry = 10; // TODO: Make configurable

    public const string TaskName = "Push Season AFK Stages";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await NavigationUtils.EnsureMainViewAsync(botApi, ct);

        // Enter "AFK Stage" main menu
        await botApi.TapAsync(new ScreenPoint(80, 1850), ct);
        await Task.Delay(2000, ct);

        Log.Information("Pushing Season AFK Stages");
        await botApi.TapAsync(new ScreenPoint(300, 1610), ct); // Tap Season AFK Stages

        var results = await BattleUtils.PushBattleStages(botApi, ct, AttemptsPerFormation, FormationsToTry);
        
        Log.Information("Finished pushing Season AFK Stages. Pushed {VictoryCount} stages in {BattleCount} battles",
            results.VictoryCount, results.TotalBattles);
    }
}
