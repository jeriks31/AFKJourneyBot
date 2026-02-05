namespace AFKJourneyBot.Common;

public sealed class ScreenFrame
{
    public ScreenFrame(byte[] pngBytes, DateTimeOffset capturedAtUtc)
    {
        if (pngBytes.Length == 0)
        {
            throw new ArgumentException("PNG bytes cannot be empty.", nameof(pngBytes));
        }

        PngBytes = pngBytes;
        CapturedAtUtc = capturedAtUtc;
    }

    public byte[] PngBytes { get; }
    public DateTimeOffset CapturedAtUtc { get; }
}

public readonly record struct ScreenPoint(int X, int Y)
{
    public ScreenPoint Add(int otherX, int otherY) => new(X + otherX, Y + otherY);
}

public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static ScreenRect FromLTRB(int left, int top, int right, int bottom)
        => new(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));

    public static ScreenRect FromXYWH(int x, int y, int width, int height)
        => new(x, y, width, height);

    public static ScreenRect Empty => new(0, 0, 0, 0);
}

public readonly record struct RgbColor(byte R, byte G, byte B);
