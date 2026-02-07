using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class LegendTrial(IBotApi botApi) : IBotTask
{
    // The "Go!" buttons in the "Legend Trial" screen
    private static readonly ScreenPoint Lightbearer = new(896, 616);
    private static readonly ScreenPoint Wilder = new(882, 894);
    private static readonly ScreenPoint Graveborn = new(930, 1190);
    private static readonly ScreenPoint Mauler = new(890, 1450);

    private const int AttemptsPerFormation = 3; // TODO: Make configurable
    private const int FormationsToTry = 10; // TODO: Make configurable
    public static string TaskName => "Legend Trial";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await NavigationUtils.EnsureMainViewAsync(botApi, ct);

        var battleModes = await botApi.WaitForTemplateAsync("battle_modes.png", ct);
        await botApi.TapAsync(battleModes!.Value, ct);

        var legendTrial = await botApi.WaitForTemplateAsync("legend_trial/legend_trial.png", ct);
        await botApi.TapAsync(legendTrial!.Value, ct);
        await Task.Delay(3000, ct);

        if ((await botApi.GetPixelAsync(Lightbearer, ct)).R < 230)
        {
            Log.Information("Entering Lightbearer tower");
            await botApi.TapAsync(Lightbearer, ct);
            await DoTower("Lightbearer", ct);
        }

        if ((await botApi.GetPixelAsync(Wilder, ct)).R < 180)
        {
            Log.Information("Entering Wilder tower");
            await botApi.TapAsync(Wilder, ct);
            await DoTower("Wilder", ct);
        }

        if ((await botApi.GetPixelAsync(Graveborn, ct)).R < 140)
        {
            Log.Information("Entering Graveborn tower");
            await botApi.TapAsync(Graveborn, ct);
            await DoTower("Graveborn", ct);
        }

        if ((await botApi.GetPixelAsync(Mauler, ct)).B < 230)
        {
            Log.Information("Entering Mauler tower");
            await botApi.TapAsync(Mauler, ct);
            await DoTower("Mauler", ct);
        }

        Log.Information("Finished all available legend trials");
    }

    private async Task<BattleStagesPushResult> DoTower(string towerName, CancellationToken ct)
    {
        var challenge = await botApi.WaitForAnyTemplateAsync(
        [
            new TemplateWait("legend_trial/challenge_1.png", "Challenge"),
            new TemplateWait("legend_trial/challenge_2.png", "Challenge"),
            new TemplateWait("legend_trial/challenge_3.png", "Challenge"),
            new TemplateWait("legend_trial/challenge_4.png", "Challenge")
        ], ct);
        await botApi.TapAsync(challenge!.Value.Point, ct);
        var results = await BattleUtils.PushBattleStages(botApi, ct, AttemptsPerFormation, FormationsToTry);
        Log.Information("Finished pushing {TowerName} tower. Pushed {VictoryCount} stages in {BattleCount} battles",
            towerName, results.VictoryCount, results.TotalBattles);

        while (await botApi.FindTemplateAsync("legend_trial/header.png", ct) is null)
        {
            await botApi.BackAsync(ct);
            await Task.Delay(2500, ct);
        }

        return results;
    }
}