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

    [Test]
    public async Task RunAsync_PerformsOneCycleInOrderAndExitsAfterCancellation()
    {
        using var cts = new CancellationTokenSource();
        var api = new ScriptedBotApi(CycleTemplates, cts);
        var task = new SolsticeClashBet(api);

        await task.RunAsync(cts.Token);

        Assert.That(api.FindCalls, Is.EqualTo(["battle_modes.png"]));
        Assert.That(api.WaitCalls.Select(call => call.Template), Is.EqualTo(CycleTemplates));
        Assert.That(api.TappedTemplates, Is.EqualTo(CycleTemplates));
        Assert.That(api.PixelCalls, Is.EqualTo(
        [
            new ScreenPoint(100, 100),
            new ScreenPoint(540, 100),
            new ScreenPoint(900, 100)
        ]));
        Assert.That(api.TextCalls, Is.EqualTo(
        [
            ScreenRect.FromXYWH(895, 40, 120, 40)
        ]));
        Assert.That(api.ObservedTokens, Has.All.EqualTo(cts.Token));

        var navigationWaits = api.WaitCalls.Take(5);
        Assert.Multiple(() =>
        {
            foreach (var wait in navigationWaits)
            {
                Assert.That(wait.Timeout, Is.Null);
                Assert.That(wait.PollInterval, Is.Null);
            }

            var resultWait = api.WaitCalls[^1];
            Assert.That(resultWait.Timeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(resultWait.PollInterval, Is.EqualTo(TimeSpan.FromSeconds(1)));
        });
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
        string tokenBalanceText = "35083") : IBotApi
    {
        private readonly Dictionary<ScreenPoint, string> _templatesByPoint = expectedTemplates
            .Select((template, index) => (Template: template, Point: new ScreenPoint(index + 1, index + 1)))
            .ToDictionary(item => item.Point, item => item.Template);
        private int _nextTemplate;

        public List<string> FindCalls { get; } = [];
        public List<WaitCall> WaitCalls { get; } = [];
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
            return Task.FromResult(new ScreenPoint(++_nextTemplate, _nextTemplate));
        }

        public Task TapAsync(ScreenPoint point, CancellationToken ct)
        {
            TappedTemplates.Add(_templatesByPoint[point]);
            ObservedTokens.Add(ct);
            if (TappedTemplates.Count == expectedTemplates.Count)
            {
                cancellationSource.Cancel();
            }

            return Task.CompletedTask;
        }

        public Task<TemplateMatch?> WaitForAnyTemplateAsync(
            IReadOnlyList<TemplateWait> candidates,
            CancellationToken ct,
            TimeSpan? timeout = null,
            TimeSpan? pollInterval = null) =>
            throw new InvalidOperationException("Unexpected WaitForAnyTemplateAsync call.");

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
}
