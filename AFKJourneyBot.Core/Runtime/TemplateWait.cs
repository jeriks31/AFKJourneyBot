using AFKJourneyBot.Common;

namespace AFKJourneyBot.Core.Runtime;

public readonly record struct TemplateWait(string Path, string Key, double Threshold = 0.92);

public readonly record struct TemplateMatch(string Key, ScreenPoint Point);
