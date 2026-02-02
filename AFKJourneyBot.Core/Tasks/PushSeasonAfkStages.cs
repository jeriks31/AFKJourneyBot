using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Definitions;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class PushSeasonAfkStages(IBotApi botApi) : IBotTask
{
    private const int AttemptsPerFormation = 1; // TODO: Make configurable
    private const int FormationsToTry = 5; // TODO: Make configurable
    public const string TaskName = "Push Season AFK Stages";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        // TODO: Handle startup from non-root view. i.e. if the game is currently deep in a menu

        // Enter "AFK Stage" main menu
        await botApi.TapAsync(new ScreenPoint(80, 1850), ct);
        await Task.Delay(2500, ct);

        Log.Information("Pushing Season AFK Stages");
        await botApi.TapAsync(new ScreenPoint(300, 1610), ct);

        var victoryCount = 0;
        var defeatsOnCurrentStage = 0;
        var previousFormationIndex = -1;
        while (defeatsOnCurrentStage < AttemptsPerFormation * FormationsToTry)
        {
            var formationIndex = defeatsOnCurrentStage / AttemptsPerFormation;
            var recordsButton = await botApi.WaitForTemplateAsync(TemplatePaths.For("records.png"), ct);
            if (formationIndex != previousFormationIndex)
            {
                Log.Information("Copying Formation #{FormationNumber}", formationIndex + 1);

                await botApi.TapAsync(recordsButton!.Value, ct);

                for (var i = 0; i < formationIndex; i++)
                {
                    var nextFormationButton =
                        await botApi.WaitForTemplateAsync(TemplatePaths.For("next_formation.png"), ct);
                    await botApi.TapAsync(nextFormationButton!.Value, ct);
                }

                var copyFormationButton =
                    await botApi.WaitForTemplateAsync(TemplatePaths.For("copy_formation.png"), ct);
                await botApi.TapAsync(copyFormationButton!.Value, ct);
                previousFormationIndex = formationIndex;
            }

            // Tap Battle
            var battleButton = await botApi.WaitForTemplateAsync(TemplatePaths.For("start_battle.png"), ct);
            Log.Information("Starting Battle #{BattleAttemptNumber}", defeatsOnCurrentStage + 1);
            await botApi.TapAsync(battleButton!.Value, ct);

            // Poll for battle defeat or victory screen
            var match = await botApi.WaitForAnyTemplateAsync(
                [
                    new TemplateWait(TemplatePaths.For("next.png"), "victory"),
                    new TemplateWait(TemplatePaths.For("rewards_increased.png"), "victory"),
                    new TemplateWait(TemplatePaths.For("retry.png"), "defeat")
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