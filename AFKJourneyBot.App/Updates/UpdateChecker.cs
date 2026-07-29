using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace AFKJourneyBot.App.Updates;

internal sealed class UpdateChecker(HttpClient httpClient, Version currentVersion, TimeSpan timeout)
{
    internal static readonly Uri LatestInfoUri =
        new("https://github.com/jeriks31/AFKJourneyBot/releases/latest/download/latest.json");

    internal async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestInfoUri);
            request.Headers.UserAgent.ParseAdd($"AFKJourneyBot/{currentVersion}");

            using var timeoutSource = new CancellationTokenSource(timeout);
            using var response = await httpClient.SendAsync(request, timeoutSource.Token);
            response.EnsureSuccessStatusCode();

            var release = await response.Content.ReadFromJsonAsync<LatestReleaseInfo>(
                cancellationToken: timeoutSource.Token);

            if (release is null ||
                !Version.TryParse(release.Version, out var latestVersion) ||
                !TryParseReleaseUri(release.ReleaseUrl, out var releaseUri))
            {
                Log.Debug(
                    "Update check skipped because latest.json had invalid content: {LatestInfo}",
                    JsonSerializer.Serialize(release));
                return null;
            }

            if (latestVersion <= currentVersion)
            {
                Log.Debug("Already up to date");
                return null;
            }

            return new UpdateInfo(currentVersion, latestVersion, releaseUri);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            Log.Debug(ex, "Update check failed.");
            return null;
        }
    }

    private static bool TryParseReleaseUri(string? value, out Uri releaseUri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            releaseUri = uri;
            return true;
        }

        releaseUri = null!;
        return false;
    }

    private sealed class LatestReleaseInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("release_url")]
        public string? ReleaseUrl { get; init; }
    }
}

internal sealed record UpdateInfo(Version CurrentVersion, Version LatestVersion, Uri ReleaseUri);
