using AFKJourneyBot.Core.Runtime;

namespace AFKJourneyBot.Tests;

public class AsyncManualResetEventTests
{
    [Test]
    public void DefaultStartsUnset()
    {
        var gate = new AsyncManualResetEvent();

        Assert.That(gate.IsSet, Is.False);
    }

    [Test]
    public async Task Set_ReleasesWaiters()
    {
        var gate = new AsyncManualResetEvent();

        var waitTask = gate.WaitAsync(CancellationToken.None);
        Assert.That(waitTask.IsCompleted, Is.False);

        gate.Set();
        await waitTask;

        Assert.That(gate.IsSet, Is.True);
    }

    [Test]
    public async Task Reset_BlocksFutureWaiters()
    {
        var gate = new AsyncManualResetEvent(set: true);

        await gate.WaitAsync(CancellationToken.None);

        gate.Reset();
        var waitTask = gate.WaitAsync(CancellationToken.None);
        Assert.That(waitTask.IsCompleted, Is.False);

        gate.Set();
        await waitTask;
    }
}
