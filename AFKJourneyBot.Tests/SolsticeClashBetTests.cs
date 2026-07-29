using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AFKJourneyBot.Tests;

public sealed class SolsticeClashBetTests
{
    private static readonly ScreenRect TokenBalanceRegion = ScreenRect.FromXYWH(898, 46, 110, 27);
    private static readonly ScreenRect BlueMmrRegion = ScreenRect.FromXYWH(235, 107, 96, 38);
    private static readonly ScreenRect RedMmrRegion = ScreenRect.FromXYWH(797, 107, 96, 38);
    private static readonly string[] CycleTemplates =
    [
        "solstice_clash/events.png",
        "solstice_clash/event_card.png",
        "solstice_clash/fortune_picks.png",
        "solstice_clash/spectate_live.png",
        "solstice_clash/red_all_in.png",
        "solstice_clash/result_back.png"
    ];
    private static readonly string[] NavigationTemplates = CycleTemplates[..5];

    [Test]
    public async Task RunAsync_PerformsOneCycleInOrderAndExitsAfterCancellation()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(CycleTemplates, cts);
        var task = new SolsticeClashBet(api);

        await task.RunAsync(cts.Token);

        Assert.That(api.FindCalls, Is.EqualTo(["battle_modes.png"]));
        Assert.That(api.WaitCalls.Select(call => call.Template), Is.EqualTo(NavigationTemplates));
        Assert.That(api.TappedTemplates, Is.EqualTo(CycleTemplates));
        Assert.That(api.PixelCalls, Is.EqualTo(
        [
            new ScreenPoint(540, 100)
        ]));
        Assert.That(api.NumberCalls, Is.EqualTo(
        [
            TokenBalanceRegion,
            BlueMmrRegion,
            RedMmrRegion
        ]));
        Assert.That(api.ObservedTokens, Has.All.EqualTo(cts.Token));

        Assert.Multiple(() =>
        {
            foreach (var wait in api.WaitCalls)
            {
                Assert.That(wait.Timeout, Is.Null);
                Assert.That(wait.PollInterval, Is.Null);
            }

            var postBetWait = api.PostBetWaitCalls.Single();
            Assert.That(postBetWait.Candidates.Select(candidate => candidate.Path), Is.EqualTo(
            [
                "solstice_clash/result_back.png",
                "battle_modes.png"
            ]));
            Assert.That(postBetWait.Candidates.Select(candidate => candidate.Key), Is.EqualTo(
            [
                "resultBack",
                "mainView"
            ]));
            Assert.That(postBetWait.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(postBetWait.PollInterval, Is.EqualTo(TimeSpan.FromSeconds(2)));
        });
    }

    [Test]
    public async Task RunAsync_BetsBlueWhenBlueMmrIsHigherAndLogsSelection()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(
            CycleTemplates,
            cts,
            blueMmrText: "5000",
            redMmrText: "4000");
        var task = new SolsticeClashBet(api);

        var logEvents = await RunWithCapturedLogsAsync(task, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(api.TappedTemplates, Is.EqualTo(
            [
                .. NavigationTemplates[..^1],
                "blue_all_in",
                CycleTemplates[^1]
            ]));
            Assert.That(api.TappedPoints[4], Is.EqualTo(new ScreenPoint(1074, 5)));
            Assert.That(
                logEvents.Any(logEvent =>
                    logEvent.Level == LogEventLevel.Information &&
                    logEvent.RenderMessage() ==
                    "Solstice Clash MMRs: blue 5000, red 4000; betting on Blue"),
                Is.True);
        });
    }

    [Test]
    public async Task RunAsync_BetsRedWhenMmrsAreTied()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(
            CycleTemplates,
            cts,
            blueMmrText: "4500",
            redMmrText: "4500");
        var task = new SolsticeClashBet(api);

        await task.RunAsync(cts.Token);

        Assert.That(api.TappedTemplates, Is.EqualTo(CycleTemplates));
    }

    [Test]
    public async Task RunAsync_FallsBackToRedAndLogsWarningWhenMmrCannotBeRead()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(
            CycleTemplates,
            cts,
            blueMmrText: "50 00",
            redMmrText: "4000");
        var task = new SolsticeClashBet(api);

        var logEvents = await RunWithCapturedLogsAsync(task, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(api.TappedTemplates, Is.EqualTo(CycleTemplates));
            Assert.That(api.NumberCalls, Has.Count.EqualTo(3));
            Assert.That(
                logEvents.Any(logEvent =>
                    logEvent.Level == LogEventLevel.Warning &&
                    logEvent.RenderMessage() ==
                    "Could not read both Solstice Clash competitor MMRs; betting on red"),
                Is.True);
        });
    }

    [Test]
    public async Task RunAsync_RestartsCycleWhenMatchReturnsToMainView()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(CycleTemplates, cts, postBetStates: ["mainView", "resultBack"]);
        var task = new SolsticeClashBet(api);

        await task.RunAsync(cts.Token);

        Assert.That(api.FindCalls, Is.EqualTo(["battle_modes.png", "battle_modes.png"]));
        Assert.That(api.PostBetWaitCalls, Has.Count.EqualTo(2));
        Assert.That(api.TappedTemplates, Is.EqualTo(
        [
            .. NavigationTemplates,
            .. CycleTemplates
        ]));
    }

    [Test]
    public async Task RunAsync_ContinuesWhenTokenBalanceCannotBeRead()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(CycleTemplates, cts, tokenBalanceText: "");
        var task = new SolsticeClashBet(api);

        await task.RunAsync(cts.Token);

        Assert.That(api.TappedTemplates, Is.EqualTo(CycleTemplates));
    }

    [Test]
    public void Catalog_RegistersTaskInEventGroup()
    {
        var descriptor = TaskCatalog.Create(new TestBotApi(), new AppConfig())
            .Single(task => task.Name == SolsticeClashBet.TaskName);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Description, Is.EqualTo(SolsticeClashBet.TaskDescription));
            Assert.That(descriptor.Group, Is.EqualTo("Event"));
            Assert.That(descriptor.CreateTask(), Is.TypeOf<SolsticeClashBet>());
        });
    }

    private sealed class ScriptedBotApi(
        IReadOnlyList<string> expectedTemplates,
        CancellationTokenSource cancellationSource,
        string tokenBalanceText = "35083",
        IEnumerable<string>? postBetStates = null,
        string blueMmrText = "4423",
        string redMmrText = "4445") : IBotApi
    {
        private readonly Dictionary<ScreenPoint, string> _templatesByPoint = expectedTemplates
            .Select((template, index) => (Template: template, Point: new ScreenPoint(index + 1, index + 1)))
            .ToDictionary(item => item.Point, item => item.Template);
        private readonly Queue<string> _postBetStates = new(postBetStates ?? ["resultBack"]);
        private int _nextTemplate;

        public List<string> FindCalls { get; } = [];
        public List<WaitCall> WaitCalls { get; } = [];
        public List<PostBetWaitCall> PostBetWaitCalls { get; } = [];
        public List<string> TappedTemplates { get; } = [];
        public List<ScreenPoint> TappedPoints { get; } = [];
        public List<ScreenPoint> PixelCalls { get; } = [];
        public List<ScreenRect> NumberCalls { get; } = [];
        public List<CancellationToken> ObservedTokens { get; } = [];

        public Task<ScreenPoint?> FindTemplateAsync(
            string relativeTemplatePath,
            CancellationToken ct,
            double threshold = 0.90)
        {
            FindCalls.Add(relativeTemplatePath);
            ObservedTokens.Add(ct);
            return Task.FromResult<ScreenPoint?>(new ScreenPoint(100, 100));
        }

        public Task<ScreenPoint> WaitForTemplateAsync(
            string relativeTemplatePath,
            CancellationToken ct,
            double threshold = 0.90,
            TimeSpan? timeout = null,
            TimeSpan? pollInterval = null)
        {
            Assert.That(relativeTemplatePath, Is.EqualTo(expectedTemplates[_nextTemplate]));
            WaitCalls.Add(new WaitCall(relativeTemplatePath, timeout, pollInterval));
            ObservedTokens.Add(ct);
            var point = new ScreenPoint(_nextTemplate + 1, _nextTemplate + 1);
            _nextTemplate = (_nextTemplate + 1) % NavigationTemplates.Length;
            return Task.FromResult(point);
        }

        public Task TapAsync(ScreenPoint point, CancellationToken ct)
        {
            TappedPoints.Add(point);
            var template = _templatesByPoint.TryGetValue(point, out var matchedTemplate)
                ? matchedTemplate
                : point == new ScreenPoint(1074, 5)
                    ? "blue_all_in"
                    : throw new InvalidOperationException($"Unexpected tap at {point}.");
            TappedTemplates.Add(template);
            ObservedTokens.Add(ct);
            if (template == expectedTemplates[^1])
            {
                cancellationSource.Cancel();
            }

            return Task.CompletedTask;
        }

        public Task<TemplateMatch?> WaitForAnyTemplateAsync(
            IReadOnlyList<TemplateWait> candidates,
            CancellationToken ct,
            TimeSpan? timeout = null,
            TimeSpan? pollInterval = null)
        {
            PostBetWaitCalls.Add(new PostBetWaitCall(candidates, timeout, pollInterval));
            ObservedTokens.Add(ct);
            var key = _postBetStates.Dequeue();
            var point = key == "resultBack" ? new ScreenPoint(6, 6) : new ScreenPoint(100, 100);
            return Task.FromResult<TemplateMatch?>(new TemplateMatch(key, point));
        }

        public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected SwipeAsync call.");

        public Task InputTextAsync(string text, CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected InputTextAsync call.");

        public Task BackAsync(CancellationToken ct) =>
            throw new InvalidOperationException("Unexpected BackAsync call.");

        public Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct)
        {
            throw new InvalidOperationException("Unexpected text OCR call.");
        }

        public Task<string> ReadNumberAsync(ScreenRect roi, CancellationToken ct)
        {
            NumberCalls.Add(roi);
            ObservedTokens.Add(ct);
            if (roi == TokenBalanceRegion)
            {
                return Task.FromResult(tokenBalanceText);
            }

            if (roi == BlueMmrRegion)
            {
                return Task.FromResult(blueMmrText);
            }

            if (roi == RedMmrRegion)
            {
                return Task.FromResult(redMmrText);
            }

            throw new InvalidOperationException($"Unexpected OCR region {roi}.");
        }

        public Task<RgbColor> GetPixelAsync(ScreenPoint point, CancellationToken ct)
        {
            PixelCalls.Add(point);
            ObservedTokens.Add(ct);
            return Task.FromResult(new RgbColor(200, 120, 40));
        }
    }

    private static async Task<IReadOnlyList<LogEvent>> RunWithCapturedLogsAsync(
        SolsticeClashBet task,
        CancellationToken ct)
    {
        var logEvents = new List<LogEvent>();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new CollectingSink(logEvents))
            .CreateLogger();
        var previousLogger = Log.Logger;

        try
        {
            Log.Logger = logger;
            await task.RunAsync(ct);
        }
        finally
        {
            Log.Logger = previousLogger;
        }

        return logEvents;
    }

    private sealed class CollectingSink(List<LogEvent> logEvents) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => logEvents.Add(logEvent);
    }

    private sealed record WaitCall(string Template, TimeSpan? Timeout, TimeSpan? PollInterval);
    private sealed record PostBetWaitCall(
        IReadOnlyList<TemplateWait> Candidates,
        TimeSpan? Timeout,
        TimeSpan? PollInterval);
}
