using System.IO;
using System.Windows;
using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;
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
        config.ValidateConfig();
        var device = new AdbDeviceController(Log.Warning, config.DeviceSerial);
        var vision = new VisionService();
        _ocr = new TesseractOcrService();
        var pauseGate = new AsyncManualResetEvent(true);
        var api = new BotApi(device, vision, _ocr, pauseGate);
        var taskManager = new TaskManager(api, pauseGate);

        var tasks = new List<TaskDescriptor>
        {
            new(PushRoutine.TaskName, () => new PushRoutine(api, config)),
            new(PushAfkStages.TaskName, () => new PushAfkStages(api, config)),
            new(PushSeasonAfkStages.TaskName, () => new PushSeasonAfkStages(api, config)),
            new(LegendTrial.TaskName, () => new LegendTrial(api, config)),
            new(HomesteadOrders.TaskName, () => new HomesteadOrders(api)),
#if DEBUG
            new(DebugTask.TaskName, () => new DebugTask(api)),
#endif
        };

        _viewModel = new MainViewModel(taskManager, tasks);
        var window = new MainWindow
        {
            DataContext = _viewModel
        };

        window.Show();

        _ = UpdateChecker.CheckForUpdateAsync(window);
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
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDirectory, "afkjourneybot-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(new UiLogSink())
            .CreateLogger();

        Log.Debug("Logger initialized");
    }
}
