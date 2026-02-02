using AFKJourneyBot.Common;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// Default implementation of bot primitives using device + vision + OCR services.
/// </summary>
public sealed class BotApi : IBotApi
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(750);
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
    public async Task<ScreenPoint?> FindTemplateAsync(string templatePath, double threshold = 0.92, CancellationToken ct = default)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return await _vision.FindTemplateAsync(screen, templatePath, threshold, ct);
    }

    /// <inheritdoc />
    public async Task<ScreenPoint?> WaitForTemplateAsync(
        string templatePath,
        double threshold = 0.92,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
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

    public Task TapAsync(int x, int y, CancellationToken ct = default)
        => RunDeviceActionAsync(() => _device.TapAsync(x, y, ct), ct);

    public Task TapAsync(ScreenPoint point, CancellationToken ct = default)
        => RunDeviceActionAsync(() => _device.TapAsync(point.X, point.Y, ct), ct);

    public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs = 250, CancellationToken ct = default)
        => RunDeviceActionAsync(() => _device.SwipeAsync(start, end, durationMs, ct), ct);

    public Task InputTextAsync(string text, CancellationToken ct = default)
        => RunDeviceActionAsync(() => _device.InputTextAsync(text, ct), ct);

    public async Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct = default)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return await _ocr.ReadTextAsync(screen, roi, ct);
    }

    public async Task<RgbColor> GetPixelAsync(int x, int y, CancellationToken ct = default)
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
