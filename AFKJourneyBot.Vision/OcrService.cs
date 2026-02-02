using AFKJourneyBot.Common;
using OpenCvSharp;
using Tesseract;

namespace AFKJourneyBot.Vision;

public interface IOcrService
{
    Task<string> ReadTextAsync(ScreenFrame screen, ScreenRect roi, CancellationToken ct);
}

public sealed class NullOcrService : IOcrService
{
    public Task<string> ReadTextAsync(ScreenFrame screen, ScreenRect roi, CancellationToken ct)
        => Task.FromResult(string.Empty);
}

public sealed class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly SemaphoreSlim _engineLock = new(1, 1);

    public TesseractOcrService(string? tessdataPath = null, string language = "eng")
    {
        var resolvedPath = string.IsNullOrWhiteSpace(tessdataPath)
            ? Path.Combine(AppContext.BaseDirectory, "tessdata")
            : tessdataPath;

        _engine = new TesseractEngine(resolvedPath, language, EngineMode.Default);
    }

    public async Task<string> ReadTextAsync(ScreenFrame screen, ScreenRect roi, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var source = screen.ToMat();
        if (source.Empty())
        {
            return string.Empty;
        }

        var rect = ClampRect(roi, source.Width, source.Height);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return string.Empty;
        }

        using var cropped = new Mat(source, rect);
        using var gray = new Mat();
        Cv2.CvtColor(cropped, gray, ColorConversionCodes.BGR2GRAY);
        using var thresh = new Mat();
        Cv2.Threshold(gray, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        Cv2.ImEncode(".png", thresh, out var pngBytes);
        using var pix = Pix.LoadFromMemory(pngBytes);

        await _engineLock.WaitAsync(ct);
        try
        {
            using var page = _engine.Process(pix);
            return page.GetText()?.Trim() ?? string.Empty;
        }
        finally
        {
            _engineLock.Release();
        }
    }

    public void Dispose()
    {
        _engine.Dispose();
        _engineLock.Dispose();
    }

    private static OpenCvSharp.Rect ClampRect(ScreenRect roi, int width, int height)
    {
        if (roi.IsEmpty)
        {
            return new OpenCvSharp.Rect();
        }

        var x = Math.Clamp(roi.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(roi.Y, 0, Math.Max(0, height - 1));
        var w = Math.Clamp(roi.Width, 0, width - x);
        var h = Math.Clamp(roi.Height, 0, height - y);

        return new OpenCvSharp.Rect(x, y, w, h);
    }
}
