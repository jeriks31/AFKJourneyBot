using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;

namespace AFKJourneyBot.Core.Tasks;

public static class TaskCatalog
{
    public static IReadOnlyList<TaskDescriptor> Create(IBotApi api, AppConfig config)
    {
        return
        [
            new(
                PushRoutine.TaskName,
                PushRoutine.TaskDescription,
                "Routine",
                () => new PushRoutine(api, config)),
            new(
                PushAfkStages.TaskName,
                PushAfkStages.TaskDescription,
                "Battle",
                () => new PushAfkStages(api, config)),
            new(
                PushSeasonAfkStages.TaskName,
                PushSeasonAfkStages.TaskDescription,
                "Battle",
                () => new PushSeasonAfkStages(api, config)),
            new(
                LegendTrial.TaskName,
                LegendTrial.TaskDescription,
                "Battle",
                () => new LegendTrial(api, config)),
            new(
                HomesteadOrders.TaskName,
                HomesteadOrders.TaskDescription,
                "Homestead",
                () => new HomesteadOrders(api)),
            new(
                SolsticeClashBet.TaskName,
                SolsticeClashBet.TaskDescription,
                "Event",
                () => new SolsticeClashBet(api)),
#if DEBUG
            new(
                DebugTask.TaskName,
                DebugTask.TaskDescription,
                "Development",
                () => new DebugTask(api)),
#endif
        ];
    }
}

public sealed record TaskDescriptor(string Name, string Description, string Group, Func<IBotTask> CreateTask);
