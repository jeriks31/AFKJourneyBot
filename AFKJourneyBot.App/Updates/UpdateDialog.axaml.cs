using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AFKJourneyBot.App.Updates;

internal sealed partial class UpdateDialog : Window
{
    internal UpdateDialog(UpdateInfo update)
    {
        InitializeComponent();
        CurrentVersionText.Text = FormatVersion(update.CurrentVersion);
        LatestVersionText.Text = FormatVersion(update.LatestVersion);
    }

    private static string FormatVersion(Version version)
        => version.Build >= 0 ? version.ToString(3) : version.ToString();

    private void NotNowClicked(object? sender, RoutedEventArgs e) => Close(false);

    private void ViewReleaseClicked(object? sender, RoutedEventArgs e) => Close(true);
}
