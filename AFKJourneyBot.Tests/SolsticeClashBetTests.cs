using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;

namespace AFKJourneyBot.Tests;

public sealed class SolsticeClashBetTests
{
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
        Assert.That(api.TextCalls, Is.EqualTo(
        [
            ScreenRect.FromXYWH(895, 40, 120, 40)
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
            Assert.That(postBetWait.Timeout, Is.EqualTo(TimeSpan.FromMinutes(3)));
            Assert.That(postBetWait.PollInterval, Is.EqualTo(TimeSpan.FromSeconds(2)));
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
        IEnumerable<string>? postBetStates = null) : IBotApi
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
        public List<ScreenPoint> PixelCalls { get; } = [];
        public List<ScreenRect> TextCalls { get; } = [];
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
            var template = _templatesByPoint[point];
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
            TextCalls.Add(roi);
            ObservedTokens.Add(ct);
            return Task.FromResult(tokenBalanceText);
        }

        public Task<RgbColor> GetPixelAsync(ScreenPoint point, CancellationToken ct)
        {
            PixelCalls.Add(point);
            ObservedTokens.Add(ct);
            return Task.FromResult(new RgbColor(200, 120, 40));
        }
    }

    private sealed record WaitCall(string Template, TimeSpan? Timeout, TimeSpan? PollInterval);
    private sealed record PostBetWaitCall(
        IReadOnlyList<TemplateWait> Candidates,
        TimeSpan? Timeout,
        TimeSpan? PollInterval);
}
