using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.UI.Logging;
using Serilog;

namespace AFKJourneyBot.UI;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TaskManager _taskManager;
    private FlowDocument _logsDocument = new();
    private int _lastLogCount;
    private bool _isRunning;
    private bool _isPaused;
    private readonly NotifyCollectionChangedEventHandler _logsChangedHandler;

    public MainViewModel(TaskManager taskManager, IEnumerable<TaskDescriptor> tasks)
    {
        _taskManager = taskManager;
        Tasks = new ObservableCollection<TaskDescriptor>(tasks);

        RunTaskCommand = new RelayCommand<TaskDescriptor>(RunTask);
        PauseCommand = new RelayCommand(_ => _taskManager.TogglePause());
        StopCommand = new RelayCommand(_ => _taskManager.Stop());

        _taskManager.StateChanged += (_, _) => UpdateState();
        UpdateState();

        _logsChangedHandler = (_, e) => UpdateLogsDocument(e);
        if (LogStore.Entries is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged += _logsChangedHandler;
        }
        UpdateLogsDocument(null);
    }

    public ObservableCollection<LogEntry> Logs { get; } = LogStore.Entries;
    public ObservableCollection<TaskDescriptor> Tasks { get; }

    public ICommand RunTaskCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }

    public string PauseButtonText => IsPaused ? "Resume" : "Pause";

    public FlowDocument LogsDocument
    {
        get => _logsDocument;
        private set
        {
            if (ReferenceEquals(value, _logsDocument))
            {
                return;
            }

            _logsDocument = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (value == _isRunning)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (value == _isPaused)
            {
                return;
            }

            _isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PauseButtonText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        if (LogStore.Entries is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= _logsChangedHandler;
        }
    }

    private void RunTask(TaskDescriptor? descriptor)
    {
        if (descriptor == null)
        {
            return;
        }

        if (_taskManager.IsRunning)
        {
            Log.Warning("Task already running.");
            return;
        }

        var task = descriptor.CreateTask();
        _ = _taskManager.RunTaskAsync(task).ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    Log.Error(t.Exception, "Task execution failed.");
                }
            },
            TaskScheduler.Default);
    }

    private void UpdateState()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            SetState();
        }
        else
        {
            dispatcher.Invoke(SetState);
        }
    }

    private void SetState()
    {
        IsRunning = _taskManager.IsRunning;
        IsPaused = _taskManager.IsPaused;
    }

    private void UpdateLogsDocument(NotifyCollectionChangedEventArgs? change)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            SetLogsDocument(change);
        }
        else
        {
            dispatcher.Invoke(() => SetLogsDocument(change));
        }
    }

    private void SetLogsDocument(NotifyCollectionChangedEventArgs? change)
    {
        if (change?.Action == NotifyCollectionChangedAction.Add &&
            change.NewItems != null &&
            _logsDocument.Blocks.Count == _lastLogCount)
        {
            foreach (LogEntry entry in change.NewItems)
            {
                AppendLogEntry(_logsDocument, entry);
            }
            _lastLogCount = Logs.Count;
            return;
        }

        var document = new FlowDocument
        {
            PagePadding = new Thickness(0)
        };

        foreach (var entry in Logs)
        {
            AppendLogEntry(document, entry);
        }

        _lastLogCount = Logs.Count;
        LogsDocument = document;
    }

    private static void AppendLogEntry(FlowDocument document, LogEntry entry)
    {
        var paragraph = new Paragraph(new Run(entry.Message))
        {
            Margin = new Thickness(0)
        };

        var brush = GetLogBrush(entry.Level);
        if (brush != null)
        {
            paragraph.Foreground = brush;
        }

        document.Blocks.Add(paragraph);
    }

    private static Brush? GetLogBrush(string level)
    {
        var resources = Application.Current?.Resources;
        if (resources == null)
        {
            return null;
        }

        return level switch
        {
            "Warning" => resources["Brush.LogWarning"] as Brush,
            "Error" => resources["Brush.LogError"] as Brush,
            "Fatal" => resources["Brush.LogError"] as Brush,
            _ => resources["Brush.LogInfo"] as Brush
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
