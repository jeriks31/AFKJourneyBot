using AFKJourneyBot.Core.Runtime;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class SampleTask : IBotTask
{
    public const string TaskName = "Sample Task";
    public string Name => TaskName;

    private readonly IBotApi _api;

    public SampleTask(IBotApi api)
    {
        _api = api;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Information("Hello World");

        while (!ct.IsCancellationRequested)
        {
            var point = await _api.WaitForTemplateAsync("button.png", ct);

            if (point != null)
            {
                await _api.TapAsync(point.Value, ct);
            }

            await Task.Delay(5000, ct);
        }
    }
}
