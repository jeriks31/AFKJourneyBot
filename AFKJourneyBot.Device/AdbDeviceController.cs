using System.Diagnostics;
using AFKJourneyBot.Common;

namespace AFKJourneyBot.Device;

public interface IDeviceController
{
    Task<ScreenFrame> ScreenshotAsync(CancellationToken ct);
    Task TapAsync(int x, int y, CancellationToken ct);
    Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct);
    Task InputTextAsync(string text, CancellationToken ct);
}

public sealed class AdbDeviceController : IDeviceController
{
    private readonly string _adbPath;
    private string? _deviceSerial;
    private readonly Action<string> _warningLogger;
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    public AdbDeviceController(Action<string> warningLogger)
    {
        _adbPath = Path.Combine(AppContext.BaseDirectory, "platform-tools", "adb.exe");
        _warningLogger = warningLogger;
    }

    public async Task<ScreenFrame> ScreenshotAsync(CancellationToken ct)
    {
        await EnsureDeviceSelectedAsync(ct);
        var pngBytes = await RunAdbForBinaryOutputAsync(BuildArgs("exec-out screencap -p"), ct);
        return new ScreenFrame(pngBytes, DateTimeOffset.UtcNow);
    }

    public Task TapAsync(int x, int y, CancellationToken ct)
        => RunAdbShellAsync($"input tap {x} {y}", ct);

    public Task SwipeAsync(ScreenPoint start, ScreenPoint end, int durationMs, CancellationToken ct)
        => RunAdbShellAsync($"input swipe {start.X} {start.Y} {end.X} {end.Y} {durationMs}", ct);

    public Task InputTextAsync(string text, CancellationToken ct)
        => RunAdbShellAsync($"input text {EscapeInputText(text)}", ct);

    private string BuildArgs(string args)
        => string.IsNullOrWhiteSpace(_deviceSerial) ? args : $"-s {_deviceSerial} {args}";

    private async Task RunAdbShellAsync(string command, CancellationToken ct)
    {
        await EnsureDeviceSelectedAsync(ct);
        await RunAdbForTextOutputAsync(BuildArgs($"shell {command}"), ct);
    }

    private async Task<byte[]> RunAdbForBinaryOutputAsync(string arguments, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(CommandTimeout);

        using var process = CreateProcess(arguments);
        process.Start();

        var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
        await using var output = new MemoryStream();

        try
        {
            await process.StandardOutput.BaseStream.CopyToAsync(output, 81920, timeoutCts.Token);
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"adb failed ({process.ExitCode}): {stderr}");
        }

        return output.ToArray();
    }

    private async Task<string> RunAdbForTextOutputAsync(string arguments, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(CommandTimeout);

        using var process = CreateProcess(arguments);
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"adb failed ({process.ExitCode}): {stderr}");
        }

        return stdout;
    }

    private Process CreateProcess(string arguments)
        => new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

    private async Task EnsureDeviceSelectedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_deviceSerial))
        {
            return;
        }

        var output = await RunAdbForTextOutputAsync("devices", ct);
        var devices = ParseDeviceSerials(output);
        if (devices.Count == 0)
        {
            _warningLogger?.Invoke("No devices detected.");
            return;
        }

        _deviceSerial = devices[0];
        if (devices.Count > 1)
        {
            _warningLogger?.Invoke($"Multiple devices detected. Using {_deviceSerial}.");
        }
    }

    private static List<string> ParseDeviceSerials(string output)
    {
        var devices = new List<string>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            if (string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
            {
                devices.Add(parts[0]);
            }
        }

        return devices;
    }

    private static string EscapeInputText(string text)
        => text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(" ", "%s", StringComparison.Ordinal)
            .Replace("&", "\\&", StringComparison.Ordinal);

    private static void TryKill(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }
}
