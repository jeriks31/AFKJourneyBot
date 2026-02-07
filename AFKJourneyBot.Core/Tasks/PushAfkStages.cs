using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class PushAfkStages(IBotApi botApi, AppConfig config) : IBotTask
{
    private readonly AppConfig.BattleTaskConfig _config = config.PushAfkStages!;
    public const string TaskName = "Push AFK Stages";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await NavigationUtils.EnsureMainViewAsync(botApi, ct);

        // Enter "AFK Stage" main menu
        await botApi.TapAsync(new ScreenPoint(80, 1850), ct);
        await Task.Delay(2000, ct);

        Log.Information("Pushing AFK Stages");
        await botApi.TapAsync(new ScreenPoint(800, 1610), ct); // Tap Regular AFK Stages

        var results = await BattleUtils.PushBattleStages(
            botApi,
            ct,
            _config.AttemptsPerFormation,
            _config.FormationsToTry);

        Log.Information("Finished pushing AFK Stages. Pushed {VictoryCount} stages in {BattleCount} battles",
            results.VictoryCount, results.TotalBattles);
    }
}
