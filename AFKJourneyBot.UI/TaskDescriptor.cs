using AFKJourneyBot.Core.Tasks;

namespace AFKJourneyBot.UI;

public sealed class TaskDescriptor
{
    public TaskDescriptor(string name, Func<IBotTask> createTask)
    {
        Name = name;
        CreateTask = createTask;
    }

    public string Name { get; }
    public Func<IBotTask> CreateTask { get; }
}
