using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Serilog;

namespace AFKJourneyBot.UI;

public static class UpdateChecker
{
    private static readonly Uri LatestInfoUri =
        new("https://github.com/jeriks31/AFKJourneyBot/releases/latest/download/latest.json");

    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task CheckForUpdateAsync(Window owner)
    {
#if DEBUG
        Log.Debug("Update check skipped in Debug builds.");
        return;
#endif

        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (currentVersion is null)
        {
            Log.Debug("Update check skipped because current version is unavailable.");
            return;
        }

        LatestReleaseInfo? latestInfo;
        try
        {
            using var client = new HttpClient();
            client.Timeout = HttpTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"AFKJourneyBot/{currentVersion}");

            latestInfo = await client.GetFromJsonAsync<LatestReleaseInfo>(LatestInfoUri, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Update check failed.");
            return;
        }

        if (latestInfo?.Version is null || latestInfo.ReleaseUrl is null ||
            !Version.TryParse(latestInfo.Version, out var latestVersion))
        {
            Log.Debug("Update check skipped because latest.json had invalid content: {LatestInfo}",
                JsonSerializer.Serialize(latestInfo));
            return;
        }

        if (latestVersion <= currentVersion)
        {
            Log.Debug("Already up to date");
            return;
        }

        var message =
            $"A newer version of AFKJourneyBot is available.\n\n" +
            $"Current: {currentVersion}\n" +
            $"Latest: {latestVersion}\n\n" +
            "Download the latest version now?";

        await owner.Dispatcher.InvokeAsync(() =>
        {
            if (ShowUpdatePrompt(owner, message))
            {
                TryOpenUrl(latestInfo.ReleaseUrl);
            }
        });
    }

    private static bool ShowUpdatePrompt(Window owner, string message)
    {
        var result = MessageBox.Show(owner, message, "AFKJourneyBot Update", MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        return result == MessageBoxResult.Yes;
    }

    private static void TryOpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open update URL.");
        }
    }

    private sealed class LatestReleaseInfo
    {
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("release_url")] public string? ReleaseUrl { get; set; }
        [JsonPropertyName("download_url")] public string? DownloadUrl { get; set; }
    }
}
