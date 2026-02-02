namespace AFKJourneyBot.Core.Definitions;

public static class TemplatePaths
{
    private static string TemplatesRoot => Path.Combine(AppContext.BaseDirectory, "templates");

    /// <summary>
    /// Get the path to a given template by filename
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string For(string fileName) => Path.Combine(TemplatesRoot, fileName);
}
