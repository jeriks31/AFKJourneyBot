using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using AFKJourneyBot.Common;
using Serilog;
using System.Globalization;

namespace AFKJourneyBot.Core.Tasks;

public sealed class SolsticeClashBet(IBotApi botApi) : IBotTask
{
    private const int ScreenRightEdgeX = 1079;
    private static readonly ScreenPoint ResultColorSamplePoint = new(540, 100);
    private static readonly ScreenRect TokenBalanceRegion = ScreenRect.FromXYWH(898, 46, 110, 27);
    private static readonly ScreenRect BlueMmrRegion = ScreenRect.FromXYWH(235, 107, 96, 38);
    private static readonly ScreenRect RedMmrRegion = ScreenRect.FromXYWH(797, 107, 96, 38);

    private const string EventsTemplate = "solstice_clash/events.png";
    private const string EventCardTemplate = "solstice_clash/event_card.png";
    private const string FortunePicksTemplate = "solstice_clash/fortune_picks.png";
    private const string SpectateLiveTemplate = "solstice_clash/spectate_live.png";
    private const string RedAllInTemplate = "solstice_clash/red_all_in.png";
    private const string ResultBackTemplate = "solstice_clash/result_back.png";
    private const string MainViewTemplate = "battle_modes.png";
    private static readonly TimeSpan ResultWaitTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ResultPollInterval = TimeSpan.FromSeconds(2);

    public const string TaskName = "Solstice Clash Bet";
    public const string TaskDescription =
        "Repeatedly bets all available tokens on the higher-rated competitor in the first live Solstice Clash match.";

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
            var redAllIn = await botApi.WaitForTemplateAsync(RedAllInTemplate, ct);
            var betSide = await SelectBetSideAsync(ct);
            var allInCoords = betSide == BetSide.Blue
                ? redAllIn with { X = ScreenRightEdgeX - redAllIn.X }
                : redAllIn;
            await botApi.TapAsync(allInCoords, ct);

            var postBetState = await botApi.WaitForAnyTemplateAsync(
            [
                new TemplateWait(ResultBackTemplate, "resultBack", 0.90),
                new TemplateWait(MainViewTemplate, "mainView", 0.95)
            ],
                ct,
                timeout: ResultWaitTimeout,
                pollInterval: ResultPollInterval);
            if (postBetState is null)
            {
                throw new TemplateWaitTimeoutException(ResultBackTemplate, ResultWaitTimeout);
            }

            if (postBetState.Value.Key == "mainView")
            {
                Log.Warning(
                    "Returned to the main view before a Solstice Clash result appeared; restarting betting cycle");
                continue;
            }

            var resultBack = postBetState.Value.Point;

            var result = await IsVictoryAsync(ct) ? "victory" : "loss";
            Log.Information("Solstice Clash bet result: {Result}", result);

            await botApi.TapAsync(resultBack, ct);
        }
    }

    private async Task LogTokenBalanceAsync(CancellationToken ct)
    {
        await Task.Delay(2000, ct); // Let UI settle
        var text = await botApi.ReadNumberAsync(TokenBalanceRegion, ct);
        if (TryParseNumber(text, out var tokenBalance))
        {
            Log.Information("Current Solstice Clash token balance: {TokenBalance}", tokenBalance);
            return;
        }

        Log.Warning("Could not read the current Solstice Clash token balance; continuing task");
    }

    private async Task<BetSide> SelectBetSideAsync(CancellationToken ct)
    {
        var blueText = await botApi.ReadNumberAsync(BlueMmrRegion, ct);
        var redText = await botApi.ReadNumberAsync(RedMmrRegion, ct);

        if (TryParseNumber(blueText, out var blueMmr) && TryParseNumber(redText, out var redMmr))
        {
            var side = blueMmr > redMmr ? BetSide.Blue : BetSide.Red;
            Log.Information("Solstice Clash MMRs: blue {BlueMmr}, red {RedMmr}; betting on {BetSide}", blueMmr, redMmr,
                side);
            return side;
        }

        Log.Warning("Could not read both Solstice Clash competitor MMRs; betting on red");
        return BetSide.Red;
    }

    private static bool TryParseNumber(string text, out long value)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private async Task<bool> IsVictoryAsync(CancellationToken ct)
    {
        var color = await botApi.GetPixelAsync(ResultColorSamplePoint, ct);
        var redBlueBias = color.R - color.B;

        return redBlueBias > 0;
    }

    private async Task WaitAndTapAsync(string template, CancellationToken ct)
    {
        var point = await botApi.WaitForTemplateAsync(template, ct);
        await botApi.TapAsync(point, ct);
    }

    private enum BetSide
    {
        Blue,
        Red
    }
}
