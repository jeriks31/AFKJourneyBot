using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks.Shared;

public static class NavigationUtils
{
    private static readonly ScreenPoint SwapWorldHomesteadPoint = new(1010, 1620);

    /// <summary>
    /// Presses "Back" until we're all the way out at the main game view (where you control your character)
    /// Can be either Homestead or Open World
    /// </summary>
    /// <param name="botApi"></param>
    /// <param name="ct"></param>
    /// <param name="maxBacks"></param>
    /// <param name="delayMs"></param>
    public static async Task EnsureMainViewAsync(
        IBotApi botApi,
        CancellationToken ct)
    {
        Log.Information("Navigating to World/Homestead game view");
        await BackUntilOut(botApi, ct);
    }

    public static async Task EnsureMainViewWorldAsync(IBotApi botApi, CancellationToken ct)
    {
        Log.Information("Navigating to World game view");
        await BackUntilOut(botApi, ct);
        var worldButton = await botApi.FindTemplateAsync("world.png", ct);
        if (worldButton is not null)
        {
            await botApi.TapAsync(SwapWorldHomesteadPoint, ct);
        }
    }

    public static async Task EnsureMainViewHomesteadAsync(IBotApi botApi, CancellationToken ct)
    {
        Log.Information("Navigating to Homestead game view");
        await BackUntilOut(botApi, ct);
        var worldButton = await botApi.FindTemplateAsync("world.png", ct);
        if (worldButton is null)
        {
            await botApi.TapAsync(SwapWorldHomesteadPoint, ct);
        }
    }

    private static async Task BackUntilOut(IBotApi botApi, CancellationToken ct, int maxBacks = 10,
        int delayMs = 2000)
    {
        for (var i = 0; i < maxBacks; i++)
        {
            if (await botApi.FindTemplateAsync("battle_modes.png", ct, threshold: 0.95) is not null)
            {
                return;
            }

            await botApi.BackAsync(ct);
            await Task.Delay(delayMs, ct);
        }

        Log.Error(
            "Main game view could not be found. The game may be unresponsive, or the UI changed in a recent update");
    }
}
