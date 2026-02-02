using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Device;
using AFKJourneyBot.UI.Logging;
using Serilog;

namespace AFKJourneyBot.UI;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TaskManager _taskManager;
    private readonly IDeviceController _device;
    private readonly int _previewIntervalMs;
    private CancellationTokenSource? _previewCts;
    private ImageSource? _previewImage;
    private DateTimeOffset _lastPreviewFrameAt = DateTimeOffset.MinValue;
    private bool _isRunning;
    private bool _isPaused;

    public MainViewModel(TaskManager taskManager, IDeviceController device, IEnumerable<TaskDescriptor> tasks, int previewIntervalMs)
    {
        _taskManager = taskManager;
        _device = device;
        _previewIntervalMs = Math.Max(250, previewIntervalMs);
        Tasks = new ObservableCollection<TaskDescriptor>(tasks);

        RunTaskCommand = new RelayCommand<TaskDescriptor>(RunTask);
        PauseCommand = new RelayCommand(_ => _taskManager.TogglePause());
        StopCommand = new RelayCommand(_ => _taskManager.Stop());

        _taskManager.StateChanged += (_, _) => UpdateState();
        UpdateState();
        StartPreview();
    }

    public ObservableCollection<LogEntry> Logs { get; } = LogStore.Entries;
    public ObservableCollection<TaskDescriptor> Tasks { get; }

    public ICommand RunTaskCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }

    public string PauseButtonText => IsPaused ? "Resume" : "Pause";

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (Equals(value, _previewImage))
            {
                return;
            }

            _previewImage = value;
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
        _previewCts?.Cancel();
        _previewCts?.Dispose();
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

    private void StartPreview()
    {
        _previewCts = new CancellationTokenSource();
        _ = Task.Run(() => PreviewLoopAsync(_previewCts.Token));
    }

    private async Task PreviewLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var frame = _device.MostRecentScreenFrame;
                if (frame == null || frame.CapturedAtUtc == _lastPreviewFrameAt)
                {
                    await Task.Delay(_previewIntervalMs, ct);
                    continue;
                }

                _lastPreviewFrameAt = frame.CapturedAtUtc;
                var image = CreateBitmapImage(frame.PngBytes);
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                {
                    PreviewImage = image;
                }
                else
                {
                    await dispatcher.InvokeAsync(() => PreviewImage = image);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Preview loop failed.");
                await Task.Delay(2000, ct);
            }

            await Task.Delay(_previewIntervalMs, ct);
        }
    }

    private static BitmapImage CreateBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
