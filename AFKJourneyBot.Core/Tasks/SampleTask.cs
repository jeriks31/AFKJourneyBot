using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Definitions;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class SampleTask : IBotTask
{
    public string Name => "Sample Task";

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
            var point = await _api.FindTemplateAsync(TemplatePaths.For("button.png"), ct: ct);

            if (point != null)
            {
                await _api.TapAsync(point.Value, ct);
            }

            await Task.Delay(5000, ct);
        }
    }
}
