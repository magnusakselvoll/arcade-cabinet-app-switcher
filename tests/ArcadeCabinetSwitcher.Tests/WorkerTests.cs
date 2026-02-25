using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class WorkerTests
{
    [TestMethod]
    public async Task Worker_StartsAndStops_WithoutError()
    {
        var worker = new Worker(NullLogger<Worker>.Instance);
        using var cts = new CancellationTokenSource();

        var executeTask = worker.StartAsync(cts.Token);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.IsTrue(executeTask.IsCompleted);
    }
}
