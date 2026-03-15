using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using ArcadeCabinetSwitcher.ProcessManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class WorkerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ProfileConfig MakeProfile(string name, string[]? commands = null, ProfileAction? action = null) =>
        new()
        {
            Name = name,
            Commands = commands?.Select(c => new CommandConfig { Command = c }).ToArray(),
            Action = action,
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

    private static AppSwitcherConfig MakeConfig(string defaultProfile, params ProfileConfig[] profiles) =>
        new() { DefaultProfile = defaultProfile, Profiles = profiles };

    private static Worker MakeWorker(
        AppSwitcherConfig config,
        out StubInputHandler inputHandler,
        out SpyProcessManager processManager,
        out SpySystemActionHandler systemActionHandler)
    {
        inputHandler = new StubInputHandler();
        processManager = new SpyProcessManager();
        systemActionHandler = new SpySystemActionHandler();

        return new Worker(
            NullLogger<Worker>.Instance,
            new StubConfigurationLoader(config),
            inputHandler,
            processManager,
            systemActionHandler);
    }

    // ── startup ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Worker_StartsAndStops_WithoutError()
    {
        var config = MakeConfig("default", MakeProfile("default", ["notepad.exe"]));
        var worker = MakeWorker(config, out _, out _, out _);
        using var cts = new CancellationTokenSource();

        var executeTask = worker.StartAsync(cts.Token);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.IsTrue(executeTask.IsCompleted);
    }

    [TestMethod]
    public async Task Worker_LaunchesDefaultProfile_OnStartup()
    {
        var config = MakeConfig("game1",
            MakeProfile("game1", ["game.exe"]),
            MakeProfile("menu", ["menu.exe"]));
        var worker = MakeWorker(config, out _, out var pm, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await pm.WaitForLaunchAsync(); // wait for default "game1" to be launched
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(1, pm.LaunchedProfiles.Count);
        Assert.AreEqual("game1", pm.LaunchedProfiles[0]);
    }

    [TestMethod]
    public async Task Worker_DefaultProfileNotFound_DoesNotCrash()
    {
        var config = MakeConfig("missing", MakeProfile("other", ["app.exe"]));
        var worker = MakeWorker(config, out _, out var pm, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(0, pm.LaunchedProfiles.Count);
    }

    // ── profile switch ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ProfileSwitch_TerminatesActiveAndLaunchesNewProfile()
    {
        var config = MakeConfig("game1",
            MakeProfile("game1", ["game.exe"]),
            MakeProfile("menu", ["menu.exe"]));
        var worker = MakeWorker(config, out var ih, out var pm, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await ih.WhenStarted;
        await pm.WaitForLaunchAsync(); // wait for default "game1" to be launched

        ih.RaiseProfileSwitchRequested("menu");
        await pm.WaitForLaunchAsync(); // wait for "menu" to be launched

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(2, pm.TerminateCallCount); // 1 from profile switch + 1 from StopAsync
        Assert.AreEqual(2, pm.LaunchedProfiles.Count);
        Assert.AreEqual("menu", pm.LaunchedProfiles[1]);
    }

    [TestMethod]
    public async Task ProfileSwitch_SpecialAction_CallsSystemActionHandler()
    {
        var config = MakeConfig("game1",
            MakeProfile("game1", ["game.exe"]),
            MakeProfile("reboot-profile", action: ProfileAction.Reboot));
        var worker = MakeWorker(config, out var ih, out var pm, out var sa);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await ih.WhenStarted;
        await pm.WaitForLaunchAsync(); // wait for default "game1" to be launched

        ih.RaiseProfileSwitchRequested("reboot-profile");
        await Task.Delay(50);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(2, pm.TerminateCallCount); // 1 from profile switch + 1 from StopAsync
        Assert.AreEqual(1, sa.ExecutedActions.Count);
        Assert.AreEqual(ProfileAction.Reboot, sa.ExecutedActions[0]);
        // LaunchProfileAsync should NOT have been called for the action profile
        Assert.AreEqual(1, pm.LaunchedProfiles.Count); // only the default
    }

    [TestMethod]
    public async Task ProfileSwitch_UnknownProfile_LogsErrorAndDoesNotCrash()
    {
        var config = MakeConfig("game1", MakeProfile("game1", ["game.exe"]));
        var worker = MakeWorker(config, out var ih, out var pm, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await ih.WhenStarted;

        ih.RaiseProfileSwitchRequested("does-not-exist");
        await Task.Delay(50);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(1, pm.TerminateCallCount); // 0 from failed switch + 1 from StopAsync
    }

    [TestMethod]
    public async Task ProfileSwitch_ConcurrentRequests_SecondIsIgnored()
    {
        var config = MakeConfig("game1",
            MakeProfile("game1", ["game.exe"]),
            MakeProfile("menu", ["menu.exe"]));

        var blockingPm = new BlockingProcessManager();
        var ih = new StubInputHandler();
        var worker = new Worker(
            NullLogger<Worker>.Instance,
            new StubConfigurationLoader(config),
            ih,
            blockingPm,
            new SpySystemActionHandler());

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await ih.WhenStarted; // wait for subscription to be set up + default launch

        blockingPm.Block(); // next TerminateActiveProfileAsync will hang

        ih.RaiseProfileSwitchRequested("menu"); // first switch — will block
        await Task.Delay(20);

        ih.RaiseProfileSwitchRequested("menu"); // second switch — should be ignored
        await Task.Delay(20);

        blockingPm.Unblock();
        await Task.Delay(50);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(2, blockingPm.TerminateCallCount, "Second switch should have been ignored (count includes 1 from StopAsync)"); // 1 from first switch + 1 from StopAsync
    }

    // ── shutdown ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task StopAsync_TerminatesActiveProfile()
    {
        var config = MakeConfig("game1", MakeProfile("game1", ["game.exe"]));
        var worker = MakeWorker(config, out _, out var pm, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.AreEqual(1, pm.TerminateCallCount);
    }

    [TestMethod]
    public async Task StopAsync_StopsInputHandler()
    {
        var config = MakeConfig("game1", MakeProfile("game1", ["game.exe"]));
        var worker = MakeWorker(config, out var ih, out _, out _);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.IsTrue(ih.StopCalled);
    }

    // ── stubs / spies ─────────────────────────────────────────────────────────

    private sealed class StubConfigurationLoader(AppSwitcherConfig config) : IConfigurationLoader
    {
        public AppSwitcherConfig Load() => config;
    }

    private sealed class StubInputHandler : IInputHandler
    {
        private readonly TaskCompletionSource _startedTcs = new();

        public event EventHandler<string>? ProfileSwitchRequested;

        /// <summary>Completes when the Worker calls StartAsync (i.e., the event subscription is set up).</summary>
        public Task WhenStarted => _startedTcs.Task;

        public bool StopCalled { get; private set; }

        public void RaiseProfileSwitchRequested(string profileName) =>
            ProfileSwitchRequested?.Invoke(this, profileName);

        public Task StartAsync(AppSwitcherConfig config, CancellationToken cancellationToken)
        {
            _startedTcs.TrySetResult();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyProcessManager : IProcessManager
    {
        private readonly List<string> _launchedProfiles = [];
        private readonly SemaphoreSlim _launchSignal = new(0);

        public IReadOnlyList<string> LaunchedProfiles => _launchedProfiles;
        public int TerminateCallCount { get; private set; }

        public async Task WaitForLaunchAsync()
            => await _launchSignal.WaitAsync(TimeSpan.FromSeconds(2));

        public Task LaunchProfileAsync(ProfileConfig profile, CancellationToken cancellationToken)
        {
            _launchedProfiles.Add(profile.Name);
            _launchSignal.Release();
            return Task.CompletedTask;
        }

        public Task TerminateActiveProfileAsync(CancellationToken cancellationToken)
        {
            TerminateCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class SpySystemActionHandler : ISystemActionHandler
    {
        private readonly List<ProfileAction> _executedActions = [];
        public IReadOnlyList<ProfileAction> ExecutedActions => _executedActions;

        public Task ExecuteAsync(ProfileAction action, CancellationToken cancellationToken)
        {
            _executedActions.Add(action);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A process manager whose TerminateActiveProfileAsync can be blocked to simulate a long-running switch.
    /// </summary>
    private sealed class BlockingProcessManager : IProcessManager
    {
        private readonly List<string> _launchedProfiles = [];
        private TaskCompletionSource? _gate;

        public int TerminateCallCount { get; private set; }

        public void Block() => _gate = new TaskCompletionSource();
        public void Unblock() => _gate?.TrySetResult();

        public Task LaunchProfileAsync(ProfileConfig profile, CancellationToken cancellationToken)
        {
            _launchedProfiles.Add(profile.Name);
            return Task.CompletedTask;
        }

        public async Task TerminateActiveProfileAsync(CancellationToken cancellationToken)
        {
            TerminateCallCount++;
            if (_gate is { } gate)
                await gate.Task;
        }
    }
}
