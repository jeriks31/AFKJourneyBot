using System.Text.Json;
using Serilog;

namespace AFKJourneyBot.Common;

public sealed class AppConfig
{
    public const int DefaultAttemptsPerFormation = 2;
    public const int DefaultFormationsToTry = 10;

    public string? DeviceSerial { get; set; }
    public BattleTaskConfig? LegendTrial { get; set; }
    public BattleTaskConfig? PushAfkStages { get; set; }
    public BattleTaskConfig? PushSeasonAfkStages { get; set; }

    public sealed class BattleTaskConfig
    {
        public int AttemptsPerFormation { get; set; } = DefaultAttemptsPerFormation;
        public int FormationsToTry { get; set; } = DefaultFormationsToTry;
    }

    public void ValidateConfig()
    {
        LegendTrial = ValidateBattleTask(LegendTrial, nameof(LegendTrial));
        PushAfkStages = ValidateBattleTask(PushAfkStages, nameof(PushAfkStages));
        PushSeasonAfkStages = ValidateBattleTask(PushSeasonAfkStages, nameof(PushSeasonAfkStages));
    }

    private static BattleTaskConfig ValidateBattleTask(BattleTaskConfig? config, string name)
    {
        if (config is null)
        {
            return new BattleTaskConfig();
        }

        if (config.AttemptsPerFormation <= 0)
        {
            Log.Error("{TaskName}.AttemptsPerFormation must be > 0. Value: {Value}.", name,
                config.AttemptsPerFormation);
            throw new InvalidOperationException(
                $"{name}.AttemptsPerFormation must be > 0. Value: {config.AttemptsPerFormation}.");
        }

        if (config.FormationsToTry <= 0)
        {
            Log.Error("{TaskName}.FormationsToTry must be > 0. Value: {Value}.", name, config.FormationsToTry);
            throw new InvalidOperationException(
                $"{name}.FormationsToTry must be > 0. Value: {config.FormationsToTry}.");
        }

        if (config.FormationsToTry > DefaultFormationsToTry)
        {
            Log.Error("{TaskName}.FormationsToTry must be <= {MaxValue}. Value: {Value}.", name,
                DefaultFormationsToTry, config.FormationsToTry);
            throw new InvalidOperationException(
                $"{name}.FormationsToTry must be <= {DefaultFormationsToTry}. Value: {config.FormationsToTry}.");
        }

        return config;
    }

    public static AppConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config.json");

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
