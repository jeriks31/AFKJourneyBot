# AFK Journey Bot — Task Authoring Guide

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
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;
using AFKJourneyBot.Core.Definitions;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public sealed class DailyQuestTask : IBotTask
{
    public const string TaskName = "Daily Quests";
    public string Name => TaskName;
    private readonly IBotApi _api;

    public DailyQuestTask(IBotApi api)
    {
        _api = api;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Information("Daily quest task started");

        var menu = await _api.WaitForTemplateAsync(
            TemplatePaths.For("main_menu.png"),
            timeout: TimeSpan.FromSeconds(10),
            ct: ct);

        if (menu == null)
        {
            Log.Warning("Main menu not found, aborting.");
            return;
        }

        await _api.TapAsync(menu.Value, ct);
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

## 4) Use templates and screen regions

Place template images in `AFKJourneyBot.UI/templates/` so they are copied to the app output.
For OCR or pixel checks, define screen regions and use `ScreenRect`:

```csharp
var rect = ScreenRect.FromXYWH(100, 200, 220, 40);
var text = await _api.ReadTextAsync(rect, ct);
```

## 5) Important rules (pause/stop safety)

To keep Pause and Stop working correctly:

- Always use **IBotApi** methods (`TapAsync`, `WaitForTemplateAsync`, `ReadTextAsync`, etc.).
- Always pass the **cancellation token** to delays or loops: `await Task.Delay(500, ct);`.
- Avoid direct ADB/vision calls from tasks (unless you understand the pause/stop consequences).

## 6) Debugging tips

- All tasks should log key steps using Serilog: `Log.Information(...)`.
- Use the preview window to verify coordinates and template matches.

## 7) Common patterns

**Tap a button if it appears**

```csharp
var point = await _api.FindTemplateAsync(TemplatePaths.For("button.png"), ct: ct);
if (point != null)
{
    await _api.TapAsync(point.Value, ct);
}
```

**Wait and tap**

```csharp
var point = await _api.WaitForTemplateAsync(TemplatePaths.For("open.png"), ct: ct);
if (point != null)
{
    await _api.TapAsync(point.Value, ct);
}
```

**Wait for a screen to appear**

```csharp
var ok = await _api.WaitForTemplateAsync(TemplatePaths.For("screen_marker.png"), timeout: TimeSpan.FromSeconds(8), ct: ct);
if (ok == null) return;
```

---
