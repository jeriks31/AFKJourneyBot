using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks;

namespace AFKJourneyBot.Tests;

public class TaskManagerTests
{
    [Test]
    public async Task RunTaskAsync_CompletesAndResetsState()
    {
        var manager = new TaskManager(new TestBotApi());
        var stateChanges = 0;
        manager.StateChanged += (_, _) => stateChanges++;

        await manager.RunTaskAsync(new ImmediateTask(), "Immediate");

        Assert.That(manager.IsRunning, Is.False);
        Assert.That(stateChanges, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task Stop_CancelsRunningTask()
    {
        var manager = new TaskManager(new TestBotApi());
        var blocking = new BlockingTask();

        var runTask = manager.RunTaskAsync(blocking, "Blocking");

        await WaitUntilAsync(() => manager.IsRunning, TimeSpan.FromSeconds(1));
        manager.Stop();

        await runTask;

        Assert.That(manager.IsRunning, Is.False);
        Assert.That(await CompletedWithinAsync(blocking.Canceled, TimeSpan.FromSeconds(1)), Is.True);
    }

    [Test]
    public async Task RunTaskAsync_ThrowsWhenAlreadyRunning()
    {
        var manager = new TaskManager(new TestBotApi());
        var blocking = new BlockingTask();

        var runTask = manager.RunTaskAsync(blocking, "Blocking");
        await WaitUntilAsync(() => manager.IsRunning, TimeSpan.FromSeconds(1));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.RunTaskAsync(new ImmediateTask(), "Immediate"));
        Assert.That(ex?.Message, Is.EqualTo("A task is already running."));

        manager.Stop();
        await runTask;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!predicate())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new AssertionException("Timed out waiting for condition.");
            }

            await Task.Delay(10);
        }
    }

    private static async Task<bool> CompletedWithinAsync(Task task, TimeSpan timeout)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeout));
        return ReferenceEquals(winner, task);
    }

    private sealed class ImmediateTask : IBotTask
    {
        public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class BlockingTask : IBotTask
    {
        private readonly TaskCompletionSource<bool> _canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Canceled => _canceled.Task;

        public async Task RunAsync(CancellationToken ct)
        {
            using var _ = ct.Register(() => _canceled.TrySetResult(true));
            await Task.Delay(Timeout.Infinite, ct);
        }
    }
}
