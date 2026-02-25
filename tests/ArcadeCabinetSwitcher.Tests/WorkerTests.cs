using Microsoft.Extensions.Logging.Abstractions;

namespace ArcadeCabinetSwitcher.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_StartsAndStops_WithoutError()
    {
        var worker = new Worker(NullLogger<Worker>.Instance);
        using var cts = new CancellationTokenSource();

        var executeTask = worker.StartAsync(cts.Token);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.True(executeTask.IsCompleted);
    }
}
