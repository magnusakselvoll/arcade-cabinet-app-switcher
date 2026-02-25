using ArcadeCabinetSwitcher.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class WorkerTests
{
    [TestMethod]
    public async Task Worker_StartsAndStops_WithoutError()
    {
        var worker = new Worker(NullLogger<Worker>.Instance, new StubConfigurationLoader());
        using var cts = new CancellationTokenSource();

        var executeTask = worker.StartAsync(cts.Token);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.IsTrue(executeTask.IsCompleted);
    }

    private sealed class StubConfigurationLoader : IConfigurationLoader
    {
        public AppSwitcherConfig Load() => new()
        {
            DefaultProfile = "default",
            Profiles =
            [
                new ProfileConfig
                {
                    Name = "default",
                    Commands = ["notepad.exe"],
                    SwitchCombo = new SwitchComboConfig
                    {
                        Buttons = ["Button1"],
                        HoldDurationSeconds = 3
                    }
                }
            ]
        };
    }
}
