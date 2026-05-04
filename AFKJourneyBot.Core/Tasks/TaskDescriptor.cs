namespace AFKJourneyBot.Core.Tasks;

public sealed class TaskDescriptor
{
    public TaskDescriptor(string name, string description, string group, Func<IBotTask> createTask)
    {
        Name = name;
        Description = description;
        Group = group;
        CreateTask = createTask;
    }

    public string Name { get; }
    public string Description { get; }
    public string Group { get; }
    public Func<IBotTask> CreateTask { get; }
}
