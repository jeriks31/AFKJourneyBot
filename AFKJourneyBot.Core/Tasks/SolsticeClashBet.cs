using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using AFKJourneyBot.Common;
using Serilog;
using System.Globalization;

namespace AFKJourneyBot.Core.Tasks;

public sealed class SolsticeClashBet(IBotApi botApi) : IBotTask
{
    private static readonly ScreenPoint[] ResultColorSamplePoints =
    [
        new(100, 100),
        new(540, 100),
        new(900, 100)
    ];
    private static readonly ScreenRect TokenBalanceRegion = ScreenRect.FromXYWH(895, 40, 120, 40);

    private const string EventsTemplate = "solstice_clash/events.png";
    private const string EventCardTemplate = "solstice_clash/event_card.png";
    private const string FortunePicksTemplate = "solstice_clash/fortune_picks.png";
    private const string SpectateLiveTemplate = "solstice_clash/spectate_live.png";
    private const string RedAllInTemplate = "solstice_clash/red_all_in.png";
    private const string ResultBackTemplate = "solstice_clash/result_back.png";

    public const string TaskName = "Solstice Clash Bet";
    public const string TaskDescription =
        "Repeatedly bets all available tokens on the red side of the first live Solstice Clash match.";

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await NavigationUtils.EnsureMainViewAsync(botApi, ct);

            await WaitAndTapAsync(EventsTemplate, ct);
            await WaitAndTapAsync(EventCardTemplate, ct);

            var fortunePicks = await botApi.WaitForTemplateAsync(FortunePicksTemplate, ct);
            await LogTokenBalanceAsync(ct);
            await botApi.TapAsync(fortunePicks, ct);

            await WaitAndTapAsync(SpectateLiveTemplate, ct);
            await WaitAndTapAsync(RedAllInTemplate, ct);

            var resultBack = await botApi.WaitForTemplateAsync(
                ResultBackTemplate,
                ct,
                timeout: TimeSpan.FromMinutes(10),
                pollInterval: TimeSpan.FromSeconds(1));

            var result = await IsVictoryAsync(ct) ? "victory" : "loss";
            Log.Information("Solstice Clash bet result: {Result}", result);

            await botApi.TapAsync(resultBack, ct);
        }
    }

    private async Task LogTokenBalanceAsync(CancellationToken ct)
    {
        await Task.Delay(1000, ct); // Let UI settle
        var text = await botApi.ReadTextAsync(TokenBalanceRegion, ct);
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        if (long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var tokenBalance))
        {
            Log.Information("Current Solstice Clash token balance: {TokenBalance}", tokenBalance);
            return;
        }

        Log.Warning("Could not read the current Solstice Clash token balance; continuing task");
    }

    private async Task<bool> IsVictoryAsync(CancellationToken ct)
    {
        var redBlueBias = 0;
        foreach (var point in ResultColorSamplePoints)
        {
            var color = await botApi.GetPixelAsync(point, ct);
            redBlueBias += color.R - color.B;
        }

        return redBlueBias > 0;
    }

    private async Task WaitAndTapAsync(string template, CancellationToken ct)
    {
        var point = await botApi.WaitForTemplateAsync(template, ct);
        await botApi.TapAsync(point, ct);
    }
}
