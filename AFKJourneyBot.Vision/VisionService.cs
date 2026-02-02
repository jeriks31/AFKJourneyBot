using AFKJourneyBot.Common;
using OpenCvSharp;

namespace AFKJourneyBot.Vision;

public interface IVisionService
{
    Task<ScreenPoint?> FindTemplateAsync(ScreenFrame screen, string templatePath, double threshold = 0.92, CancellationToken ct = default);
    RgbColor GetPixel(ScreenFrame screen, int x, int y);
}

public static class ScreenFrameCvExtensions
{
    public static Mat ToMat(this ScreenFrame frame)
    {
        if (frame.PngBytes.Length == 0)
        {
            throw new ArgumentException("Frame contains no PNG data.", nameof(frame));
        }

        return Cv2.ImDecode(frame.PngBytes, ImreadModes.Color);
    }
}

public sealed class VisionService : IVisionService
{
    public Task<ScreenPoint?> FindTemplateAsync(ScreenFrame screen, string templatePath, double threshold = 0.92, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Template image not found.", templatePath);
        }

        using var source = screen.ToMat();
        if (source.Empty())
        {
            return Task.FromResult<ScreenPoint?>(null);
        }

        using var template = Cv2.ImRead(templatePath, ImreadModes.Color);
        if (template.Empty())
        {
            throw new InvalidOperationException($"Failed to load template image: {templatePath}");
        }

        using var result = new Mat();
        Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

        if (maxVal < threshold)
        {
            return Task.FromResult<ScreenPoint?>(null);
        }

        var center = new ScreenPoint(
            maxLoc.X + (template.Width / 2),
            maxLoc.Y + (template.Height / 2));

        return Task.FromResult<ScreenPoint?>(center);
    }

    public RgbColor GetPixel(ScreenFrame screen, int x, int y)
    {
        using var source = screen.ToMat();
        if (source.Empty())
        {
            throw new InvalidOperationException("Empty screen frame.");
        }

        if (x < 0 || y < 0 || x >= source.Width || y >= source.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Pixel ({x},{y}) is outside the frame.");
        }

        var color = source.At<Vec3b>(y, x);
        return new RgbColor(color.Item2, color.Item1, color.Item0);
    }
}
