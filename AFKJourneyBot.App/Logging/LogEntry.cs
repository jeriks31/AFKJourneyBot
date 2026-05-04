namespace AFKJourneyBot.App.Logging;

public sealed record LogEntry(string Level, string Message)
{
    public bool IsDebug => Level is "Verbose" or "Debug";
    public bool IsInformation => Level == "Information";
    public bool IsWarning => Level == "Warning";
    public bool IsError => Level is "Error" or "Fatal";
}
