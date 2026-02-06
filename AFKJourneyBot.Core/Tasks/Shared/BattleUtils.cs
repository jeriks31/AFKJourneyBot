using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks.Shared;

public static class BattleUtils
{
    /// <summary>
    /// Loops through:
    /// 1. Open records
    /// 2. Copy n-th formation
    /// 3. Start Battle
    /// 4. Wait for Battle to end
    /// Until it reaches the defeat limit
    /// </summary>
    /// <param name="botApi"></param>
    /// <param name="ct"></param>
    /// <param name="attemptsPerFormation"></param>
    /// <param name="formationsToTry"></param>
    /// <returns></returns>
    public static async Task<BattleStagesPushResult> PushBattleStages(IBotApi botApi, CancellationToken ct,
        int attemptsPerFormation,
        int formationsToTry)
    {
        var totalBattleCount = 0;
        var victoryCount = 0;
        var defeatsOnCurrentStage = 0;
        var previousFormationIndex = -1;
        while (defeatsOnCurrentStage < attemptsPerFormation * formationsToTry)
        {
            var formationIndex = defeatsOnCurrentStage / attemptsPerFormation;
            var recordsButton = await botApi.WaitForTemplateAsync("afk_stages/records.png", ct);
            if (formationIndex != previousFormationIndex)
            {
                await botApi.TapAsync(recordsButton!.Value, ct);

                var nextFormationButton = await botApi.WaitForTemplateAsync("afk_stages/next_formation.png", ct);
                for (var i = 0; i < formationIndex; i++)
                {
                    await botApi.TapAsync(nextFormationButton!.Value, ct);
                    await Task.Delay(500, ct);
                    if (await botApi.FindTemplateAsync("afk_stages/not_owned.png", ct, threshold: 0.95) is not null)
                    {
                        Log.Debug("Hero not owned, skipping");
                        formationIndex++;
                    }
                }

                if (formationIndex+1 > formationsToTry)
                {
                    break; // Happens when "Hero not owned" occurs on the last formation
                }

                var copyFormationButton =
                    await botApi.WaitForTemplateAsync("afk_stages/copy_formation.png", ct);
                await botApi.TapAsync(copyFormationButton!.Value, ct);
                Log.Information("Copied Formation #{FormationNumber}", formationIndex + 1);
                previousFormationIndex = formationIndex;
            }

            // Tap Battle
            var battleButton = await botApi.WaitForTemplateAsync("afk_stages/start_battle.png", ct, threshold: 0.95);
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
                    break;
                case "defeat":
                    defeatsOnCurrentStage++;
                    break;
                default:
                    Log.Warning("Unknown battle result key: {Key}", match.Value.Key);
                    break;
            }

            totalBattleCount++;
            await botApi.TapAsync(new ScreenPoint(750, 1810), ct); // Go to next
        }

        return new BattleStagesPushResult(totalBattleCount, victoryCount);
    }
}