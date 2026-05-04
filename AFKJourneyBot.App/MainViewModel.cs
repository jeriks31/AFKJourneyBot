using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AFKJourneyBot.App.Logging;
using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly RelayCommand<TaskDescriptor> _runTaskCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _stopCommand;
    private TaskManager? _taskManager;
    private bool _isRunning;
    private bool _isPaused;
    private bool _isReady;
    private bool _hasStartupFailed;
    private string _statusText = "Starting...";

    public MainViewModel()
    {
        Tasks = new ObservableCollection<TaskDescriptor>();

        _runTaskCommand = new RelayCommand<TaskDescriptor>(RunTask, CanRunTask);
        _pauseCommand = new RelayCommand(_ => _taskManager?.TogglePause(), _ => IsReady);
        _stopCommand = new RelayCommand(_ => _taskManager?.Stop(), _ => IsReady);
    }

    public ObservableCollection<LogEntry> Logs { get; } = LogStore.Entries;
    public ObservableCollection<TaskDescriptor> Tasks { get; }
    public ICommand RunTaskCommand => _runTaskCommand;
    public ICommand PauseCommand => _pauseCommand;
    public ICommand StopCommand => _stopCommand;
    public string PauseButtonText => IsPaused ? "Resume" : "Pause";
    public bool IsBusyStarting => !IsReady && !HasStartupFailed;

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (value == _statusText)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (value == _isReady)
            {
                return;
            }

            _isReady = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusyStarting));
            RaiseCommandStatesChanged();
        }
    }

    public bool HasStartupFailed
    {
        get => _hasStartupFailed;
        private set
        {
            if (value == _hasStartupFailed)
            {
                return;
            }

            _hasStartupFailed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusyStarting));
            RaiseCommandStatesChanged();
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

            HasStartupFailed = false;
            StatusText = "Ready";
            IsReady = true;
            UpdateState();
        });
    }

    public void FailStartup(Exception startupException)
    {
        LogStore.DispatchToUiThread(() =>
        {
            HasStartupFailed = true;
            IsReady = false;
            StatusText = "Startup failed";
            LogStore.Add(new LogEntry("Error", $"Startup failed: {startupException.Message}"));
        });
    }

    public void Dispose()
    {
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

    private bool CanRunTask(TaskDescriptor? descriptor)
        => IsReady && descriptor != null && _taskManager is { IsRunning: false };

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
        IsRunning = _taskManager?.IsRunning ?? false;
        IsPaused = _taskManager?.IsPaused ?? false;
        RaiseCommandStatesChanged();
    }

    private void RaiseCommandStatesChanged()
    {
        _runTaskCommand.RaiseCanExecuteChanged();
        _pauseCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
