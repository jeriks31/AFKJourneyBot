namespace AFKJourneyBot.Core.Runtime;

public sealed class TemplateWaitTimeoutException : TimeoutException
{
    public TemplateWaitTimeoutException(
        string relativeTemplatePath,
        TimeSpan timeout)
        : base($"Timed out while searching for template '{relativeTemplatePath}' after {timeout.TotalSeconds:F1}s.")
    {
        RelativeTemplatePath = relativeTemplatePath;
        Timeout = timeout;
    }

    public string RelativeTemplatePath { get; }
    public TimeSpan Timeout { get; }
}
