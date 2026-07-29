using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;

namespace AFKJourneyBot.Tests;

public sealed class TestBotApi : IBotApi
{
    public Task<ScreenPoint?> FindTemplateAsync(string relativeTemplatePath, CancellationToken ct,
        double threshold = 0.99)
    {
        return Task.FromResult<ScreenPoint?>(null);
    }

    public Task<ScreenPoint> WaitForTemplateAsync(
        string relativeTemplatePath,
        CancellationToken ct,
        double threshold = 0.99,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        return Task.FromResult(new ScreenPoint(0, 0));
    }

    public Task<TemplateMatch?> WaitForAnyTemplateAsync(
        IReadOnlyList<TemplateWait> candidates,
        CancellationToken ct,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        return Task.FromResult<TemplateMatch?>(null);
    }

    public Task TapAsync(ScreenPoint point, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task InputTextAsync(string text, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task BackAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct)
    {
        return Task.FromResult("");
    }

    public Task<string> ReadNumberAsync(ScreenRect roi, CancellationToken ct)
    {
        return Task.FromResult("");
    }

    public Task<RgbColor> GetPixelAsync(ScreenPoint point, CancellationToken ct)
    {
        return Task.FromResult(new RgbColor(0, 0, 0));
    }

    public Task<string> SaveScreenshotAsync(CancellationToken ct, string? label = null)
    {
        return Task.FromResult("");
    }
}
