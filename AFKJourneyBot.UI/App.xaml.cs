using System.IO;
using System.Windows;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;
using AFKJourneyBot.UI.Config;
using AFKJourneyBot.UI.Logging;
using Serilog;

namespace AFKJourneyBot.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IOcrService? _ocr;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigureLogging();

        try
        {
            PlatformToolsBootstrapper.EnsureAvailable(Log.Information, Log.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to prepare Android platform-tools.");
            MessageBox.Show(
                "AFKJourneyBot couldn't download Android platform-tools.\n" +
                "Please check your internet connection or place platform-tools in the app folder and restart.",
                "AFKJourneyBot",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var config = AppConfig.Load();
        var device = new AdbDeviceController(Log.Warning);
        var vision = new VisionService();
        _ocr = new TesseractOcrService();
        var pauseGate = new AsyncManualResetEvent(true);
        var api = new BotApi(device, vision, _ocr, pauseGate);
        var taskManager = new TaskManager(api, pauseGate);

        var tasks = new List<TaskDescriptor>
        {
            new(PushRoutine.TaskName, () => new PushRoutine(api)),
            new(PushAfkStages.TaskName, () => new PushAfkStages(api)),
            new(PushSeasonAfkStages.TaskName, () => new PushSeasonAfkStages(api)),
            new(HomesteadOrders.TaskName, () => new HomesteadOrders(api))
        };

        _viewModel = new MainViewModel(taskManager, device, tasks, config.PreviewIntervalMs);
        var window = new MainWindow
        {
            DataContext = _viewModel
        };

        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        if (_ocr is IDisposable disposable)
        {
            disposable.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureLogging()
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "afkjourneybot-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(new UiLogSink())
            .CreateLogger();

        Log.Information("Logger initialized");
    }
}
