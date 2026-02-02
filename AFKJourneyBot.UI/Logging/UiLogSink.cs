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
        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VERB",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => logEvent.Level.ToString().ToUpperInvariant()
        };
        var line = $"[{logEvent.Timestamp:HH:mm:ss}][{level}] {message}";

        if (logEvent.Exception != null)
        {
            line += $" | {logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}";
        }

        LogStore.Add(new LogEntry(logEvent.Level.ToString(), line));
    }
}
