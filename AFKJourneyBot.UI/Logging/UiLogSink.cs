using Serilog.Core;
using Serilog.Events;

namespace AFKJourneyBot.UI.Logging;

public sealed class UiLogSink : ILogEventSink
{
    private readonly IFormatProvider? _formatProvider;

    public UiLogSink(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_formatProvider);
        var line = $"{logEvent.Timestamp:HH:mm:ss} {message}";

        if (logEvent.Exception != null)
        {
            line += $" | {logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";
        }

        LogStore.Add(new LogEntry(logEvent.Level.ToString(), line));
    }
}
