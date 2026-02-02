using AFKJourneyBot.Common;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// Default implementation of bot primitives using device + vision + OCR services.
/// </summary>
public sealed class BotApi : IBotApi
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);
    private readonly IDeviceController _device;
    private readonly IVisionService _vision;
    private readonly IOcrService _ocr;
    private readonly AsyncManualResetEvent _pauseGate;

    /// <summary>
    /// Creates a bot API backed by device, vision, and OCR services.
    /// </summary>
    public BotApi(IDeviceController device, IVisionService vision, IOcrService ocr, AsyncManualResetEvent pauseGate)
    {
        _device = device;
        _vision = vision;
        _ocr = ocr;
        _pauseGate = pauseGate;
    }

    /// <inheritdoc />
    public async Task<ScreenPoint?> FindTemplateAsync(string templatePath, CancellationToken ct, double threshold = 0.92)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return await _vision.FindTemplateAsync(screen, templatePath, threshold, ct);
    }

    /// <inheritdoc />
    public async Task<ScreenPoint?> WaitForTemplateAsync(
        string templatePath,
        CancellationToken ct,
        double threshold = 0.92,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? DefaultPollInterval;
        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            await EnsureNotPausedAsync(ct);
            var screen = await _device.ScreenshotAsync(ct);
            var point = await _vision.FindTemplateAsync(screen, templatePath, threshold, ct);
            if (point != null)
            {
                return point;
            }

            if (timeout.HasValue && DateTimeOffset.UtcNow - start >= timeout.Value)
            {
                return null;
            }

            await Task.Delay(interval, ct);
        }
    }

    public async Task<TemplateMatch?> WaitForAnyTemplateAsync(
        IReadOnlyList<TemplateWait> candidates,
        CancellationToken ct,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        if (candidates == null || candidates.Count == 0)
        {
            throw new ArgumentException("At least one template must be provided.", nameof(candidates));
        }

        var interval = pollInterval ?? DefaultPollInterval;
        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            await EnsureNotPausedAsync(ct);
            var screen = await _device.ScreenshotAsync(ct);

            foreach (var candidate in candidates)
            {
                var point = await _vision.FindTemplateAsync(screen, candidate.Path, candidate.Threshold, ct);
                if (point != null)
                {
                    return new TemplateMatch(candidate.Key, point.Value);
                }
            }

            if (timeout.HasValue && DateTimeOffset.UtcNow - start >= timeout.Value)
            {
                return null;
            }

            await Task.Delay(interval, ct);
        }
    }

    public Task TapAsync(int x, int y, CancellationToken ct)
        => RunDeviceActionAsync(() => _device.TapAsync(x, y, ct), ct);

    public Task TapAsync(ScreenPoint point, CancellationToken ct)
        => RunDeviceActionAsync(() => _device.TapAsync(point.X, point.Y, ct), ct);

    public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct)
        => RunDeviceActionAsync(() => _device.SwipeAsync(start, end, durationMs, ct), ct);

    public Task InputTextAsync(string text, CancellationToken ct)
        => RunDeviceActionAsync(() => _device.InputTextAsync(text, ct), ct);

    public async Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return await _ocr.ReadTextAsync(screen, roi, ct);
    }

    public async Task<RgbColor> GetPixelAsync(int x, int y, CancellationToken ct)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return _vision.GetPixel(screen, x, y);
    }

    /// <summary>
    /// Waits for the pause gate to be open.
    /// </summary>
    private async Task EnsureNotPausedAsync(CancellationToken ct)
    {
        await _pauseGate.WaitAsync(ct);
    }

    /// <summary>
    /// Runs a device action after honoring the pause gate.
    /// </summary>
    private async Task RunDeviceActionAsync(Func<Task> action, CancellationToken ct)
    {
        await EnsureNotPausedAsync(ct);
        await action();
    }
}
