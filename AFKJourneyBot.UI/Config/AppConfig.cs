using System.IO;
using System.Text.Json;
using Serilog;

namespace AFKJourneyBot.UI.Config;

public sealed class AppConfig
{
    public string? DeviceSerial { get; set; }
    public int PreviewIntervalMs { get; set; } = 1000;

    public static AppConfig Load(string? configPath = null)
    {
        var path = configPath ?? Path.Combine(AppContext.BaseDirectory, "config.json");
        if (!File.Exists(path))
        {
            Log.Warning("Config file not found at {ConfigPath}. Using defaults.", path);
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read config. Using defaults.");
            return new AppConfig();
        }
    }
}
