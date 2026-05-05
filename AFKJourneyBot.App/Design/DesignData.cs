using System.Collections.ObjectModel;
using System.Windows.Input;
using AFKJourneyBot.App.Logging;

namespace AFKJourneyBot.App.Design;

public static class DesignData
{
    public static DesignMainViewModel MainWindow { get; } = new();
}

public sealed class DesignMainViewModel
{
    public DesignMainViewModel()
    {
        RunTaskCommand = new RelayCommand(_ => { });
        StopCommand = new RelayCommand(_ => { }, _ => false);

        Tasks =
        [
            new DesignTaskDescriptor(
                "Push Routine",
                "Runs Legend Trial, Season AFK stages, and regular AFK stages in sequence."),
            new DesignTaskDescriptor(
                "Push AFK Stages",
                "Pushes regular AFK stages."),
            new DesignTaskDescriptor(
                "Push Season AFK Stages",
                "Pushes Season AFK stages."),
            new DesignTaskDescriptor(
                "Legend Trial",
                "Pushes available Legend Trial towers."),
            new DesignTaskDescriptor(
                "Homestead Orders",
                "Completes Homestead production orders."),
            new DesignTaskDescriptor(
                "Debug",
                "Runs a lightweight development task used to verify runtime wiring and logging."),
        ];

        Logs =
        [
            new LogEntry("Debug", "[01:23:45][DEBUG] This is a debug log"),
            new LogEntry("Information", "[01:23:45][INFO] This is an info log"),
            new LogEntry("Warning", "[01:23:45][WARNING] This is a warning log"),
            new LogEntry("Error", "[01:23:45][ERROR] This is an error log")
        ];
    }

    public string ActiveTaskName { get; } = "Push Routine";
    public string ActiveTaskElapsedTime { get; } = "00:12:34";
    public ObservableCollection<DesignTaskDescriptor> Tasks { get; }
    public ObservableCollection<LogEntry> Logs { get; }
    public ICommand RunTaskCommand { get; }
    public ICommand StopCommand { get; }
}

public sealed record DesignTaskDescriptor(string Name, string Description);
