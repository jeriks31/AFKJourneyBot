using System.Collections.ObjectModel;
using System.Windows;

namespace AFKJourneyBot.UI.Logging;

public static class LogStore
{
    private const int MaxEntries = 500;

    public static ObservableCollection<LogEntry> Entries { get; } = new();

    public static void Add(LogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            AddInternal(entry);
            return;
        }

        dispatcher.BeginInvoke(() => AddInternal(entry));
    }

    private static void AddInternal(LogEntry entry)
    {
        Entries.Add(entry);
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(0);
        }
    }
}
