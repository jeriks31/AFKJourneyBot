using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks.Shared;

public static class NavigationUtils
{
    public static async Task EnsureMainViewAsync(
        IBotApi botApi,
        CancellationToken ct,
        int maxBacks = 10,
        int delayMs = 2000)
    {
        Log.Information("Navigating to main game view");

        for (var i = 0; i < maxBacks; i++)
        {
            if (await botApi.FindTemplateAsync("battle_modes.png", ct) is not null)
            {
                return;
            }

            await botApi.BackAsync(ct);
            await Task.Delay(delayMs, ct);
        }

        Log.Error("Main game view could not be found. The game may be unresponsive, or the UI changed in a recent update");
    }
}
