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
    Task<ScreenPoint?> FindTemplateAsync(string templatePath, double threshold = 0.92, CancellationToken ct = default);
    /// <summary>
    /// Waits until a template is found or a timeout occurs.
    /// </summary>
    Task<ScreenPoint?> WaitForTemplateAsync(
        string templatePath,
        double threshold = 0.92,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default);
    /// <summary>
    /// Taps a screen coordinate.
    /// </summary>
    Task TapAsync(int x, int y, CancellationToken ct = default);
    /// <summary>
    /// Taps a screen coordinate.
    /// </summary>
    Task TapAsync(ScreenPoint point, CancellationToken ct = default);
    /// <summary>
    /// Swipes from start to end.
    /// </summary>
    Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs = 250, CancellationToken ct = default);
    /// <summary>
    /// Inputs text using the device input method.
    /// </summary>
    Task InputTextAsync(string text, CancellationToken ct = default);
    /// <summary>
    /// Reads OCR text from a rectangular region of the screen.
    /// </summary>
    Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct = default);
    /// <summary>
    /// Gets the RGB color of a pixel.
    /// </summary>
    Task<RgbColor> GetPixelAsync(int x, int y, CancellationToken ct = default);
}
