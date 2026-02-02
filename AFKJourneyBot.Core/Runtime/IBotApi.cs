using AFKJourneyBot.Common;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// High-level bot primitives used by tasks.
/// </summary>
public interface IBotApi
{
    /// <summary>
    /// Finds the first occurrence of a template on the current screen.
    /// </summary>
    Task<ScreenPoint?> FindTemplateAsync(string templatePath, CancellationToken ct, double threshold = 0.92);
    /// <summary>
    /// Waits until a template is found or a timeout occurs.
    /// </summary>
    Task<ScreenPoint?> WaitForTemplateAsync(
        string templatePath,
        CancellationToken ct,
        double threshold = 0.92,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null);
    /// <summary>
    /// Taps a screen coordinate.
    /// </summary>
    Task TapAsync(int x, int y, CancellationToken ct);
    /// <summary>
    /// Taps a screen coordinate.
    /// </summary>
    Task TapAsync(ScreenPoint point, CancellationToken ct);
    /// <summary>
    /// Swipes from start to end.
    /// </summary>
    Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct);
    /// <summary>
    /// Inputs text using the device input method.
    /// </summary>
    Task InputTextAsync(string text, CancellationToken ct);
    /// <summary>
    /// Reads OCR text from a rectangular region of the screen.
    /// </summary>
    Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct);
    /// <summary>
    /// Gets the RGB color of a pixel.
    /// </summary>
    Task<RgbColor> GetPixelAsync(int x, int y, CancellationToken ct);
}
