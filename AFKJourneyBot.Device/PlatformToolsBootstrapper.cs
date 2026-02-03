using System.IO.Compression;
using System.Net.Http;

namespace AFKJourneyBot.Device;

public static class PlatformToolsBootstrapper
{
    private const string PlatformToolsFolderName = "platform-tools";
    private static readonly string[] RequiredFiles =
    {
        "adb.exe",
        "AdbWinApi.dll",
        "AdbWinUsbApi.dll"
    };
    private static readonly Uri DownloadUri = new("https://dl.google.com/android/repository/platform-tools-latest-windows.zip");
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(45);

    public static string PlatformToolsDirectory => Path.Combine(AppContext.BaseDirectory, PlatformToolsFolderName);
    public static string AdbPath => Path.Combine(PlatformToolsDirectory, "adb.exe");

    public static void EnsureAvailable(Action<string> infoLogger, Action<string> warningLogger)
    {
        if (HasRequiredFiles())
        {
            return;
        }

        infoLogger.Invoke("Android platform-tools not found. Downloading...");

        var tempRoot = Path.Combine(Path.GetTempPath(), $"afkjourneybot-platform-tools-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(tempRoot, "platform-tools.zip");
        var extractRoot = Path.Combine(tempRoot, "extract");

        Directory.CreateDirectory(tempRoot);

        try
        {
            DownloadZip(zipPath);
            ZipFile.ExtractToDirectory(zipPath, extractRoot);

            var extractedToolsDir = Path.Combine(extractRoot, PlatformToolsFolderName);
            if (!Directory.Exists(extractedToolsDir))
            {
                throw new InvalidOperationException("Downloaded platform-tools archive did not contain the expected folder.");
            }

            if (Directory.Exists(PlatformToolsDirectory))
            {
                Directory.Delete(PlatformToolsDirectory, recursive: true);
            }

            CopyDirectory(extractedToolsDir, PlatformToolsDirectory);

            if (!HasRequiredFiles())
            {
                throw new InvalidOperationException("Platform-tools was missing required files after extraction.");
            }

            infoLogger.Invoke("Android platform-tools ready.");
        }
        catch (Exception ex)
        {
            warningLogger.Invoke($"Failed to download Android platform-tools: {ex.Message}");
            throw;
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static void DownloadZip(string destinationPath)
    {
        using var client = new HttpClient
        {
            Timeout = DownloadTimeout
        };

        using var response = client.GetAsync(DownloadUri, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter()
            .GetResult();
        response.EnsureSuccessStatusCode();

        using var contentStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        contentStream.CopyTo(fileStream);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destinationFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destinationFile, overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            var destinationSubDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, destinationSubDir);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool HasRequiredFiles()
    {
        foreach (var file in RequiredFiles)
        {
            if (!File.Exists(Path.Combine(PlatformToolsDirectory, file)))
            {
                return false;
            }
        }

        return true;
    }
}
