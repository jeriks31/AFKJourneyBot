# How to run (WIP)

install any android emulator with ADB support. for example bluestacks or mumuplayer. for mumuplayer adb is enabled OOTB,
in bluestacks it needs to be enabled in settings.
install and log in to AFK Journey on the emulator
grab the latest github release

# How to contribute

### Requirements (TODO: better formatting and link to .net10)
.NET 10

This project is designed so tasks are easy to add and safe to run. Tasks are small classes that depend only on the **bot API** (not on raw ADB, vision, or OCR directly). This keeps pause/stop behavior reliable and makes debugging simple.

## 1) Task interface

Every task implements `IBotTask`:

```csharp
public interface IBotTask
{
    string Name { get; }
    Task RunAsync(CancellationToken ct);
}
```

## 2) Create a new task

Create a new class under `AFKJourneyBot.Core/Tasks/`.
Declare a public static `TaskName` so the UI can list tasks without instantiating them.

Example:

```csharp
public sealed class DailyQuestTask(IBotApi botApi) : IBotTask
{
    public const string TaskName = "Daily Quests";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Information("Daily quest task started");

        var menu = await botApi.WaitForTemplateAsync("main_menu.png", ct);

        if (menu is null)
        {
            Log.Warning("Main menu not found, aborting.");
            return;
        }

        await botApi.TapAsync(menu.Value, ct);
        await Task.Delay(500, ct);

        // More steps...
    }
}
```

## 3) Register the task in the UI

Open `AFKJourneyBot.UI/App.xaml.cs` and add a new `TaskDescriptor`:

```csharp
var tasks = new List<TaskDescriptor>
{
    new(SampleTask.TaskName, () => new SampleTask(api)),
    new(DailyQuestTask.TaskName, () => new DailyQuestTask(api))
};
```

The UI will automatically render a button for each task.

## 4) Use templates

Place template images in `AFKJourneyBot.UI/templates/`.

## 5) Important rules (pause/stop safety)

To keep Pause and Stop working correctly:

- Always use **IBotApi** methods (`TapAsync`, `WaitForTemplateAsync`, `ReadTextAsync`, etc.). Avoid direct ADB/vision calls from tasks
- Always pass the **cancellation token** to delays or loops: `await Task.Delay(500, ct);`.

---
