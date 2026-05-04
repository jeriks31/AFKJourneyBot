using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AFKJourneyBot.App.Logging;
using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using AFKJourneyBot.Device;
using AFKJourneyBot.Vision;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;

namespace AFKJourneyBot.App;

public sealed partial class App : Application
{
    private IOcrService? _ocr;
    private MainViewModel? _viewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigureLogging();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => DisposeRuntime();

            _viewModel = new MainViewModel();
            window.DataContext = _viewModel;
            Log.Information("UI initialized");
            _ = InitializeRuntimeAsync(_viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeRuntimeAsync(MainViewModel viewModel)
    {
        try
        {
            Log.Debug("Preparing runtime services...");
            var runtime = await Task.Run(CreateRuntime);
            _ocr = runtime.Ocr;
            viewModel.CompleteStartup(runtime.TaskManager, runtime.Tasks);
            Log.Debug("Runtime services ready");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Startup failed.");
            viewModel.FailStartup(ex);
        }
    }

    private static RuntimeComposition CreateRuntime()
    {
        PlatformToolsBootstrapper.EnsureAvailable(Log.Information, Log.Warning);

        var config = AppConfig.Load();
        config.ValidateConfig();

        var device = new AdbDeviceController(Log.Warning, config.DeviceSerial);
        var vision = new VisionService();
        var ocr = new TesseractOcrService();
        var pauseGate = new AsyncManualResetEvent(true);
        var api = new BotApi(device, vision, ocr, pauseGate);
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

        return new RuntimeComposition(ocr, taskManager, tasks);
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

    private void DisposeRuntime()
    {
        _viewModel?.Dispose();
        if (_ocr is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
    }

    private sealed record RuntimeComposition(IOcrService Ocr, TaskManager TaskManager, IReadOnlyList<TaskDescriptor> Tasks);
}
