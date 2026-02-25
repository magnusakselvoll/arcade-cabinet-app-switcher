using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class WorkerTests
{
    [TestMethod]
    public async Task Worker_StartsAndStops_WithoutError()
    {
        var worker = new Worker(
            NullLogger<Worker>.Instance,
            new StubConfigurationLoader(),
            new StubInputHandler());
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

    private sealed class StubInputHandler : IInputHandler
    {
        public event EventHandler<string>? ProfileSwitchRequested { add { } remove { } }

        public Task StartAsync(AppSwitcherConfig config, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
