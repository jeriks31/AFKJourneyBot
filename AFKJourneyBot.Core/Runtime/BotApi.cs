using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Definitions;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;
using Serilog;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// Default implementation of bot primitives using device + vision + OCR services.
/// </summary>
public sealed class BotApi : IBotApi
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan PopupPostTapDelay = TimeSpan.FromMilliseconds(2000);
    private static readonly string[] PopupTemplateNames =
    [
        // Add popup button templates here, e.g. "popup_close.png", "popup_ok.png"
    ];
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


    public async Task<ScreenPoint?> FindTemplateAsync(string relativeTemplatePath, CancellationToken ct, double threshold = 0.92)
    {
        await EnsureNotPausedAsync(ct);
        var screen = await _device.ScreenshotAsync(ct);
        return await _vision.FindTemplateAsync(screen, TemplatePaths.For(relativeTemplatePath), threshold, ct);
    }

    public async Task<ScreenPoint?> WaitForTemplateAsync(
        string relativeTemplatePath,
        CancellationToken ct,
        double threshold = 0.92,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        pollInterval ??= DefaultPollInterval;
        timeout ??= TimeSpan.FromSeconds(60);
        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            await EnsureNotPausedAsync(ct);
            var screen = await _device.ScreenshotAsync(ct);
            if (await TryHandlePopupAsync(screen, ct))
            {
                continue;
            }
            var point = await _vision.FindTemplateAsync(screen, TemplatePaths.For(relativeTemplatePath), threshold, ct);
            if (point != null)
            {
                return point;
            }

            if (DateTimeOffset.UtcNow - start >= timeout.Value)
            {
                var debugPath = await TrySaveDebugScreenshotAsync(screen, relativeTemplatePath, ct);
                Log.Error(
                    "Timed out while searching for template {TemplatePath}. There may be an unhandled popup. Debug image saved to {DebugImagePath}",
                    relativeTemplatePath, debugPath ?? "COULD_NOT_SAVE_IMAGE");
                return null;
            }

            await Task.Delay(pollInterval.Value, ct);
        }
    }

    public async Task<TemplateMatch?> WaitForAnyTemplateAsync(
        IReadOnlyList<TemplateWait> candidates,
        CancellationToken ct,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? DefaultPollInterval;
        var start = DateTimeOffset.UtcNow;

        while (true)
        {
            await EnsureNotPausedAsync(ct);
            var screen = await _device.ScreenshotAsync(ct);
            if (await TryHandlePopupAsync(screen, ct))
            {
                continue;
            }

            foreach (var candidate in candidates.Select(c => c with { Path = TemplatePaths.For(c.Path) }))
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

    private async Task<bool> TryHandlePopupAsync(ScreenFrame screen, CancellationToken ct)
    {
        foreach (var templateName in PopupTemplateNames)
        {
            var templatePath = TemplatePaths.For(templateName);
            if (!File.Exists(templatePath))
            {
                Log.Warning("Could not find template {TemplatePath}", templatePath);
                continue;
            }

            var point = await _vision.FindTemplateAsync(screen, templatePath, 0.92, ct);
            if (point == null)
            {
                continue;
            }

            await RunDeviceActionAsync(() => _device.BackAsync(ct), ct);
            await Task.Delay(PopupPostTapDelay, ct);
            return true;
        }

        return false;
    }

    public Task TapAsync(int x, int y, CancellationToken ct) =>
        RunDeviceActionAsync(() => _device.TapAsync(x, y, ct), ct);

    public Task TapAsync(ScreenPoint point, CancellationToken ct) =>
        RunDeviceActionAsync(() => _device.TapAsync(point.X, point.Y, ct), ct);

    public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct) =>
        RunDeviceActionAsync(() => _device.SwipeAsync(start, end, durationMs, ct), ct);

    public Task InputTextAsync(string text, CancellationToken ct) =>
        RunDeviceActionAsync(() => _device.InputTextAsync(text, ct), ct);

    public Task BackAsync(CancellationToken ct) =>
        RunDeviceActionAsync(() => _device.BackAsync(ct), ct);

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

    private static async Task<string?> TrySaveDebugScreenshotAsync(
        ScreenFrame screen,
        string templatePath,
        CancellationToken ct)
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "debug_screenshots");
            Directory.CreateDirectory(folder);

            var baseName = Path.GetFileNameWithoutExtension(templatePath);

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = $"{timestamp}_{baseName}.png";
            var fullPath = Path.Combine(folder, fileName);

            await File.WriteAllBytesAsync(fullPath, screen.PngBytes, ct);
            return fullPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save debug screenshot.");
            return null;
        }
    }
}
