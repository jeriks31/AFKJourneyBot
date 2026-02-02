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

        var config = AppConfig.Load();
        var device = new AdbDeviceController(
            deviceSerial: config.DeviceSerial,
            warningLogger: message => Log.Warning(message));
        var vision = new VisionService();
        _ocr = CreateOcrService();
        var pauseGate = new AsyncManualResetEvent(true);
        var api = new BotApi(device, vision, _ocr, pauseGate);
        var taskManager = new TaskManager(api, pauseGate);

        var tasks = new List<TaskDescriptor>
        {
            new("Sample Task", () => new SampleTask(api))
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

    private static IOcrService CreateOcrService()
    {
        try
        {
            return new TesseractOcrService();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OCR disabled. Missing tessdata folder next to the app.");
            return new NullOcrService();
        }
    }

}
