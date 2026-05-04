using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace AFKJourneyBot.App.Logging;

public static class LogStore
{
    private const int MaxEntries = 500;

    public static ObservableCollection<LogEntry> Entries { get; } = new();

    public static void Add(LogEntry entry)
        => DispatchToUiThread(() => AddInternal(entry));

    public static void DispatchToUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
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
