using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.ProcessManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class ProcessManagerTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    private static ProfileConfig MakeProfile(string name, params string[] commands) =>
        new()
        {
            Name = name,
            Commands = commands.Select(c => new CommandConfig { Command = c }).ToArray(),
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

    private static ProcessManager MakeManager(StubProcessLauncher launcher, TimeSpan? timeout = null) =>
        new(NullLogger<ProcessManager>.Instance, launcher, timeout ?? ShortTimeout);

    // ── launch ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task LaunchProfileAsync_LaunchesAllCommandsInProfile()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var profile = MakeProfile("p1", "app1.exe", "app2.exe");

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(2, launcher.Launched.Count);
    }

    [TestMethod]
    public async Task LaunchProfileAsync_SpawnFailureOnOneCommand_LaunchesRemainingCommands()
    {
        var launcher = new StubProcessLauncher { FailFileName = "failing.exe" };
        var pm = MakeManager(launcher);
        var profile = MakeProfile("p1", "failing.exe", "ok.exe");

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(1, launcher.Launched.Count);
        Assert.AreEqual("ok.exe", launcher.Launched[0].FileName);
    }

    // ── graceful termination ─────────────────────────────────────────────────

    [TestMethod]
    public async Task TerminateActiveProfileAsync_CallsCloseMainWindowOnAllHandles()
    {
        var launcher = new StubProcessLauncher { AutoExitOnClose = true };
        var pm = MakeManager(launcher);
        await pm.LaunchProfileAsync(MakeProfile("p1", "app1.exe", "app2.exe"), CancellationToken.None);

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.IsTrue(launcher.Launched.All(h => h.CloseMainWindowCalled));
    }

    [TestMethod]
    public async Task TerminateActiveProfileAsync_SkipsAlreadyExitedProcesses()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        await pm.LaunchProfileAsync(MakeProfile("p1", "app.exe"), CancellationToken.None);
        launcher.Launched[0].SimulateExit();

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.IsFalse(launcher.Launched[0].CloseMainWindowCalled);
        Assert.IsFalse(launcher.Launched[0].KillCalled);
    }

    // ── force-kill ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task TerminateActiveProfileAsync_ForceKillsProcessesThatDoNotExitGracefully()
    {
        var launcher = new StubProcessLauncher(); // AutoExitOnClose = false: process never exits on CloseMainWindow
        var pm = MakeManager(launcher, ShortTimeout);
        await pm.LaunchProfileAsync(MakeProfile("p1", "stubborn.exe"), CancellationToken.None);

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.IsTrue(launcher.Launched[0].KillCalled);
    }

    // ── no-op ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task TerminateActiveProfileAsync_NoActiveProcesses_IsNoOp()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.AreEqual(0, launcher.Launched.Count);
    }

    // ── disposal ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task TerminateActiveProfileAsync_DisposesAllHandles()
    {
        var launcher = new StubProcessLauncher { AutoExitOnClose = true };
        var pm = MakeManager(launcher);
        await pm.LaunchProfileAsync(MakeProfile("p1", "app1.exe", "app2.exe"), CancellationToken.None);

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.IsTrue(launcher.Launched.All(h => h.Disposed));
    }

    [TestMethod]
    public async Task TerminateActiveProfileAsync_DisposesAlreadyExitedHandles()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        await pm.LaunchProfileAsync(MakeProfile("p1", "app.exe"), CancellationToken.None);
        launcher.Launched[0].SimulateExit();

        await pm.TerminateActiveProfileAsync(CancellationToken.None);

        Assert.IsTrue(launcher.Launched[0].Disposed);
    }

    // ── working directory tests ───────────────────────────────────────────────

    [TestMethod]
    public async Task LaunchProfileAsync_NoWorkingDirectory_UsesExecutableDirectory()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var exeDir = Path.Combine(Path.GetTempPath(), "Games", "MAME");
        var exePath = Path.Combine(exeDir, "mame64.exe");
        var profile = new ProfileConfig
        {
            Name = "p1",
            Commands = [new CommandConfig { Command = exePath }],
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(exeDir, launcher.Launched[0].WorkingDirectory);
    }

    [TestMethod]
    public async Task LaunchProfileAsync_ExplicitWorkingDirectory_UsesProvidedDirectory()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var customDir = Path.Combine(Path.GetTempPath(), "Custom", "Dir");
        var profile = new ProfileConfig
        {
            Name = "p1",
            Commands = [new CommandConfig { Command = "app.exe", WorkingDirectory = customDir }],
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(customDir, launcher.Launched[0].WorkingDirectory);
    }

    // ── delaySeconds ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task LaunchProfileAsync_CommandWithDelay_DelaysBeforeLaunch()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var profile = new ProfileConfig
        {
            Name = "p1",
            Commands =
            [
                new CommandConfig { Command = "first.exe" },
                new CommandConfig { Command = "second.exe", DelaySeconds = 1 }
            ],
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await pm.LaunchProfileAsync(profile, CancellationToken.None);
        sw.Stop();

        Assert.AreEqual(2, launcher.Launched.Count);
        Assert.IsTrue(sw.Elapsed >= TimeSpan.FromSeconds(1),
            $"Expected at least 1s delay but elapsed was {sw.Elapsed}");
    }

    [TestMethod]
    public async Task LaunchProfileAsync_CommandWithZeroDelay_LaunchesWithoutDelay()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var profile = new ProfileConfig
        {
            Name = "p1",
            Commands = [new CommandConfig { Command = "app.exe", DelaySeconds = 0 }],
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(1, launcher.Launched.Count);
    }

    [TestMethod]
    public async Task LaunchProfileAsync_CommandWithNullDelay_LaunchesWithoutDelay()
    {
        var launcher = new StubProcessLauncher();
        var pm = MakeManager(launcher);
        var profile = new ProfileConfig
        {
            Name = "p1",
            Commands = [new CommandConfig { Command = "app.exe", DelaySeconds = null }],
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 3 }
        };

        await pm.LaunchProfileAsync(profile, CancellationToken.None);

        Assert.AreEqual(1, launcher.Launched.Count);
    }

    // ── stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubProcessLauncher : IProcessLauncher
    {
        private int _nextId = 1;
        private readonly List<StubProcessHandle> _launched = [];

        public IReadOnlyList<StubProcessHandle> Launched => _launched;

        /// <summary>File name that will throw on Start (simulates failed spawn).</summary>
        public string? FailFileName { get; set; }

        /// <summary>When true, CloseMainWindow immediately marks the process as exited.</summary>
        public bool AutoExitOnClose { get; set; }

        public IProcessHandle Start(string fileName, string arguments, string? workingDirectory)
        {
            if (fileName == FailFileName)
                throw new InvalidOperationException($"Simulated launch failure for '{fileName}'.");

            var handle = new StubProcessHandle(_nextId++, fileName, workingDirectory, AutoExitOnClose);
            _launched.Add(handle);
            return handle;
        }
    }

    private sealed class StubProcessHandle(int id, string fileName, string? workingDirectory, bool autoExitOnClose) : IProcessHandle
    {
        private readonly TaskCompletionSource _exitTcs = new();

        public int Id { get; } = id;
        public string FileName { get; } = fileName;
        public string? WorkingDirectory { get; } = workingDirectory;
        public bool HasExited { get; private set; }
        public bool CloseMainWindowCalled { get; private set; }
        public bool KillCalled { get; private set; }
        public bool Disposed { get; private set; }

        public bool CloseMainWindow()
        {
            CloseMainWindowCalled = true;
            if (autoExitOnClose)
                SimulateExit();
            return true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            _exitTcs.Task.WaitAsync(cancellationToken);

        public void Kill(bool entireProcessTree)
        {
            KillCalled = true;
            SimulateExit();
        }

        public void Dispose() => Disposed = true;

        public void SimulateExit()
        {
            HasExited = true;
            _exitTcs.TrySetResult();
        }
    }
}
