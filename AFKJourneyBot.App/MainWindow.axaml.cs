using System.Collections.Specialized;
using System.Collections.ObjectModel;
using AFKJourneyBot.App.Design;
using AFKJourneyBot.App.Logging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaDesign = Avalonia.Controls.Design;

namespace AFKJourneyBot.App;

public sealed partial class MainWindow : Window
{
    private static readonly IBrush DebugLogBrush = new SolidColorBrush(Color.Parse("#8EA0B8"));
    private static readonly IBrush InformationLogBrush = new SolidColorBrush(Color.Parse("#B8C0CC"));
    private static readonly IBrush WarningLogBrush = new SolidColorBrush(Color.Parse("#F2C14E"));
    private static readonly IBrush ErrorLogBrush = new SolidColorBrush(Color.Parse("#FF6B6B"));

    private ObservableCollection<LogEntry>? _logs;

    public MainWindow()
    {
        if (AvaloniaDesign.IsDesignMode)
        {
            AvaloniaDesign.SetDataContext(this, DesignData.MainWindow);
        }

        InitializeComponent();
        DataContextChanged += (_, _) => AttachLogAutoScroll();
        Closed += (_, _) => DetachLogAutoScroll();
    }

    private void AttachLogAutoScroll()
    {
        DetachLogAutoScroll();

        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        _logs = viewModel.Logs;
        _logs.CollectionChanged += LogsChanged;
        RenderLogs();
        ScrollLogsToEnd();
    }

    private void DetachLogAutoScroll()
    {
        if (_logs == null)
        {
            return;
        }

        _logs.CollectionChanged -= LogsChanged;
        _logs = null;
    }

    private void LogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderLogs();

        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset)
        {
            ScrollLogsToEnd();
        }
    }

    private void RenderLogs()
    {
        var inlines = new InlineCollection();

        if (_logs != null)
        {
            for (var index = 0; index < _logs.Count; index++)
            {
                var entry = _logs[index];
                inlines.Add(new Run(index == _logs.Count - 1 ? entry.Message : $"{entry.Message}{Environment.NewLine}")
                {
                    Foreground = GetLogBrush(entry),
                });
            }
        }

        LogTextBlock.Inlines = inlines;
    }

    private static IBrush GetLogBrush(LogEntry entry)
        => entry.Level switch
        {
            "Verbose" or "Debug" => DebugLogBrush,
            "Warning" => WarningLogBrush,
            "Error" or "Fatal" => ErrorLogBrush,
            _ => InformationLogBrush,
        };

    private void ScrollLogsToEnd()
    {
        Dispatcher.UIThread.Post(() => LogScrollViewer.ScrollToEnd(), DispatcherPriority.Background);
    }
}
