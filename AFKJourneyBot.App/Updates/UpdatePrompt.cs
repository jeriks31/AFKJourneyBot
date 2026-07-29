using System.Diagnostics;
using Avalonia.Controls;
using Serilog;

namespace AFKJourneyBot.App.Updates;

internal static class UpdatePrompt
{
    private static readonly HttpClient HttpClient = new();
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(5);

    internal static async Task CheckOnStartupAsync(Window owner)
    {
#if DEBUG
        Log.Debug("Update check skipped in Debug builds.");
        await Task.CompletedTask;
#else
        try
        {
            var currentVersion = typeof(App).Assembly.GetName().Version!;
            var checker = new UpdateChecker(HttpClient, currentVersion, HttpTimeout);
            var update = await checker.CheckAsync();
            if (update is null)
            {
                return;
            }

            var viewRelease = await new UpdateDialog(update).ShowDialog<bool>(owner);
            if (viewRelease)
            {
                OpenReleasePage(update.ReleaseUri);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Update prompt failed.");
        }
#endif
    }

    private static void OpenReleasePage(Uri releaseUri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = releaseUri.AbsoluteUri,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open update URL.");
        }
    }
}
