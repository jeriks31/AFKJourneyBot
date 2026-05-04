using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AFKJourneyBot.App.Logging;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using Avalonia.Threading;
using Serilog;

namespace AFKJourneyBot.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly RelayCommand<TaskDescriptor> _runTaskCommand;
    private readonly RelayCommand _stopCommand;
    private TaskManager? _taskManager;
    private bool _isReady;
    private string _activeTaskName = "None";
    private string _activeTaskElapsedTime = "00:00:00";
    private DateTimeOffset? _activeTaskStartedAt;
    private readonly DispatcherTimer _elapsedTimer;

    public MainViewModel()
    {
        Tasks = [];

        _runTaskCommand = new RelayCommand<TaskDescriptor>(RunTask, CanRunTask);
        _stopCommand = new RelayCommand(_ => _taskManager?.Stop(), _ => CanStopTask());
        _elapsedTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _elapsedTimer.Tick += ElapsedTimerTick;
    }

    public ObservableCollection<LogEntry> Logs { get; } = LogStore.Entries;
    // ReSharper disable once MemberCanBePrivate.Global CollectionNeverQueried.Global
    public ObservableCollection<TaskDescriptor> Tasks { get; }
    // ReSharper disable once UnusedMember.Global
    public ICommand RunTaskCommand => _runTaskCommand;
    // ReSharper disable once UnusedMember.Global
    public ICommand StopCommand => _stopCommand;

    public string ActiveTaskName
    {
        get => _activeTaskName;
        private set
        {
            if (value == _activeTaskName)
            {
                return;
            }

            _activeTaskName = value;
            OnPropertyChanged();
        }
    }

    public string ActiveTaskElapsedTime
    {
        get => _activeTaskElapsedTime;
        private set
        {
            if (value == _activeTaskElapsedTime)
            {
                return;
            }

            _activeTaskElapsedTime = value;
            OnPropertyChanged();
        }
    }

    private bool IsReady
    {
        get => _isReady;
        set
        {
            if (value == _isReady)
            {
                return;
            }

            _isReady = value;
            RaiseCommandStatesChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void CompleteStartup(TaskManager taskManager, IEnumerable<TaskDescriptor> tasks)
    {
        LogStore.DispatchToUiThread(() =>
        {
            if (_taskManager != null)
            {
                _taskManager.StateChanged -= TaskManagerStateChanged;
            }

            _taskManager = taskManager;
            _taskManager.StateChanged += TaskManagerStateChanged;

            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }

            IsReady = true;
            UpdateState();
        });
    }

    public void FailStartup(Exception startupException)
    {
        LogStore.DispatchToUiThread(() =>
        {
            IsReady = false;
            LogStore.Add(new LogEntry("Error", $"Startup failed: {startupException.Message}"));
        });
    }

    public void Dispose()
    {
        _elapsedTimer.Stop();
        _elapsedTimer.Tick -= ElapsedTimerTick;

        if (_taskManager != null)
        {
            _taskManager.StateChanged -= TaskManagerStateChanged;
        }
    }

    private void RunTask(TaskDescriptor? descriptor)
    {
        if (descriptor == null || !CanRunTask(descriptor) || _taskManager == null)
        {
            return;
        }

        if (_taskManager.IsRunning)
        {
            Log.Warning("Task already running.");
            return;
        }

        var task = descriptor.CreateTask();
        StartActiveTask(descriptor.Name);
        _ = _taskManager.RunTaskAsync(task, descriptor.Name).ContinueWith(
            t =>
            {
                if (t.Exception != null)
                {
                    Log.Error(t.Exception, "Task execution failed.");
                }
            },
            TaskScheduler.Default);
    }

    private bool CanRunTask(TaskDescriptor? descriptor)
        => IsReady && descriptor != null && _taskManager is { IsRunning: false };

    private bool CanStopTask()
        => IsReady && _taskManager is { IsRunning: true };

    private void TaskManagerStateChanged(object? sender, EventArgs e) => UpdateState();

    private void UpdateState()
    {
        if (_taskManager == null)
        {
            return;
        }

        LogStore.DispatchToUiThread(SetState);
    }

    private void SetState()
    {
        var isRunning = _taskManager?.IsRunning ?? false;
        if (!isRunning)
        {
            StopActiveTask();
        }

        RaiseCommandStatesChanged();
    }

    private void StartActiveTask(string taskName)
    {
        ActiveTaskName = taskName;
        _activeTaskStartedAt = DateTimeOffset.Now;
        UpdateElapsedTime();
        _elapsedTimer.Start();
    }

    private void StopActiveTask()
    {
        _elapsedTimer.Stop();
        _activeTaskStartedAt = null;
        ActiveTaskName = "None";
        ActiveTaskElapsedTime = "00:00:00";
    }

    private void UpdateElapsedTime()
    {
        if (_activeTaskStartedAt == null)
        {
            ActiveTaskElapsedTime = "00:00:00";
            return;
        }

        var elapsed = DateTimeOffset.Now - _activeTaskStartedAt.Value;
        ActiveTaskElapsedTime = elapsed.ToString(elapsed.TotalDays >= 1 ? @"d\.hh\:mm\:ss" : @"hh\:mm\:ss");
    }

    private void ElapsedTimerTick(object? sender, EventArgs e) => UpdateElapsedTime();

    private void RaiseCommandStatesChanged()
    {
        _runTaskCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
