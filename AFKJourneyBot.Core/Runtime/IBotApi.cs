using AFKJourneyBot.Common;

namespace AFKJourneyBot.Core.Runtime;

/// <summary>
/// High-level bot primitives used by tasks.
/// </summary>
public interface IBotApi
{
    Task<ScreenPoint?> FindTemplateAsync(string relativeTemplatePath, CancellationToken ct, double threshold = 0.99);
    /// <summary>
    /// Waits until a template is found or a timeout occurs. Accepts a relative template path.
    /// </summary>
    Task<ScreenPoint?> WaitForTemplateAsync(
        string relativeTemplatePath,
        CancellationToken ct,
        double threshold = 0.99,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        bool errorOnFail = true);
    /// <summary>
    /// Waits until any of the provided templates is found or a timeout occurs. Accepts relative template paths.
    /// </summary>
    Task<TemplateMatch?> WaitForAnyTemplateAsync(
        IReadOnlyList<TemplateWait> candidates,
        CancellationToken ct,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null);

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
    /// Presses the 'Back' button
    /// </summary>
    Task BackAsync(CancellationToken ct);
    /// <summary>
    /// Reads OCR text from a rectangular region of the screen.
    /// </summary>
    Task<string> ReadTextAsync(ScreenRect roi, CancellationToken ct);
    /// <summary>
    /// Gets the RGB color of a pixel.
    /// </summary>
    Task<RgbColor> GetPixelAsync(ScreenPoint point, CancellationToken ct);
}
