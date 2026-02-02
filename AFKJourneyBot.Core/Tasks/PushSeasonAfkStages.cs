using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class PushSeasonAfkStages(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Push Season AFK Stages";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await PushAfkStagesRunner.RunAsync(
            botApi,
            "Season AFK Stages",
            new ScreenPoint(300, 1610),
            ct);
    }
}

public class PushAfkStages(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Push AFK Stages";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await PushAfkStagesRunner.RunAsync(
            botApi,
            "AFK Stages",
            new ScreenPoint(800, 1610),
            ct);
    }
}

internal static class PushAfkStagesRunner
{
    private const int AttemptsPerFormation = 1; // TODO: Make configurable
    private const int FormationsToTry = 5; // TODO: Make configurable

    public static async Task RunAsync(
        IBotApi botApi,
        string gameModeLabel,
        ScreenPoint entryTapPoint,
        CancellationToken ct)
    {
        Log.Information("Navigating to main game view");
        while (await botApi.FindTemplateAsync("battle_modes.png", ct) is null)
        {
            await botApi.BackAsync(ct);
            await Task.Delay(2000, ct);
        }

        // Enter "AFK Stage" main menu
        await botApi.TapAsync(new ScreenPoint(80, 1850), ct);
        await Task.Delay(2000, ct);

        Log.Information("Pushing {StageLabel}", gameModeLabel);
        await botApi.TapAsync(entryTapPoint, ct); // Tap Either Season AFK Stages or Regular AFK Stages

        var victoryCount = 0;
        var defeatsOnCurrentStage = 0;
        var previousFormationIndex = -1;
        while (defeatsOnCurrentStage < AttemptsPerFormation * FormationsToTry)
        {
            var formationIndex = defeatsOnCurrentStage / AttemptsPerFormation;
            var recordsButton = await botApi.WaitForTemplateAsync("afk_stages/records.png", ct);
            if (formationIndex != previousFormationIndex)
            {
                Log.Information("Copying Formation #{FormationNumber}", formationIndex + 1);

                await botApi.TapAsync(recordsButton!.Value, ct);

                for (var i = 0; i < formationIndex; i++)
                {
                    var nextFormationButton =
                        await botApi.WaitForTemplateAsync("afk_stages/next_formation.png", ct);
                    await botApi.TapAsync(nextFormationButton!.Value, ct);
                }

                var copyFormationButton =
                    await botApi.WaitForTemplateAsync("afk_stages/copy_formation.png", ct);
                await botApi.TapAsync(copyFormationButton!.Value, ct);
                previousFormationIndex = formationIndex;
            }

            // Tap Battle
            var battleButton = await botApi.WaitForTemplateAsync("afk_stages/start_battle.png", ct);
            Log.Information("Starting Battle #{BattleAttemptNumber}", defeatsOnCurrentStage + 1);
            await botApi.TapAsync(battleButton!.Value, ct);

            // Poll for battle defeat or victory screen
            var match = await botApi.WaitForAnyTemplateAsync(
                [
                    new TemplateWait("afk_stages/next.png", "victory"),
                    new TemplateWait("afk_stages/rewards_increased.png", "victory"),
                    new TemplateWait("afk_stages/retry.png", "defeat")
                ],
                ct);

            switch (match!.Value.Key)
            {
                case "victory":
                    victoryCount++;
                    Log.Information("Victory count: {VictoryCount}", victoryCount);
                    defeatsOnCurrentStage = 0;
                    previousFormationIndex = -1;
                    await botApi.TapAsync(new ScreenPoint(750, 1810), ct); // Go to next
                    break;
                case "defeat":
                    defeatsOnCurrentStage++;
                    await botApi.TapAsync(new ScreenPoint(750, 1810), ct);
                    break;
                default:
                    Log.Warning("Unknown battle result key: {Key}", match.Value.Key);
                    break;
            }
        }

        Log.Information("Defeat limit reached, moving on");
    }
}
