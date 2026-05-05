using System.Collections.Specialized;
using AFKJourneyBot.App.Design;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaDesign = Avalonia.Controls.Design;

namespace AFKJourneyBot.App;

public sealed partial class MainWindow : Window
{
    private INotifyCollectionChanged? _logs;

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
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset)
        {
            ScrollLogsToEnd();
        }
    }

    private void ScrollLogsToEnd()
    {
        Dispatcher.UIThread.Post(() => LogScrollViewer.ScrollToEnd(), DispatcherPriority.Background);
    }
}
