# AFK Journey Bot

Automates common AFK Journey actions by driving an Android emulator via ADB and template matching.

## Run (release build)

1) Install an Android emulator with ADB support (MuMuPlayer, BlueStacks, etc.).
2) Enable ADB in the emulator settings if required.
3) Install and log in to AFK Journey on the emulator.
4) Download the latest GitHub release and run `AFKJourneyBot.UI`.

## Contribute / develop

**Requirements**
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Task authoring

Every task implements `IBotTask`:

```csharp
public interface IBotTask
{
    string Name { get; }
    Task RunAsync(CancellationToken ct);
}
```

Create a new class under `AFKJourneyBot.Core/Tasks/`.  
Declare a public static `TaskName` so the UI can list tasks without instantiating them.

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

### Register the task in the UI

Open `AFKJourneyBot.UI/App.xaml.cs` and add a `TaskDescriptor`:

```csharp
var tasks = new List<TaskDescriptor>
{
    new(SampleTask.TaskName, () => new SampleTask(api)),
    new(DailyQuestTask.TaskName, () => new DailyQuestTask(api))
};
```

The UI renders a button for each task.

### Templates

Place template images in `AFKJourneyBot.UI/templates/`.  
`IBotApi` methods accept **relative** template paths (including subfolders), e.g.:

```csharp
await botApi.WaitForTemplateAsync("afk_stages/records.png", ct);
```

### Safety rules (pause/stop)

- Use **IBotApi** methods (`TapAsync`, `WaitForTemplateAsync`, `ReadTextAsync`, etc.). Avoid direct ADB/vision calls from tasks.
- Always pass the **cancellation token** to delays/loops: `await Task.Delay(500, ct);`.

## References

**Tesseract OCR**
- [Official docs](https://tesseract-ocr.github.io/)
- [.NET wrapper](https://github.com/charlesw/tesseract)

**OpenCV**
- [Official docs](https://docs.opencv.org/4.x/)
- [.NET wrapper](https://github.com/shimat/opencvsharp)

**Tools**
- [Pixspy](https://pixspy.com/): Useful for analyzing screenshots for button coordinates and RGB color values.