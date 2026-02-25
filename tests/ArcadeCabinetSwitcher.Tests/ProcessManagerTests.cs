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
            Commands = commands,
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

        public IProcessHandle Start(string fileName, string arguments)
        {
            if (fileName == FailFileName)
                throw new InvalidOperationException($"Simulated launch failure for '{fileName}'.");

            var handle = new StubProcessHandle(_nextId++, fileName, AutoExitOnClose);
            _launched.Add(handle);
            return handle;
        }
    }

    private sealed class StubProcessHandle(int id, string fileName, bool autoExitOnClose) : IProcessHandle
    {
        private readonly TaskCompletionSource _exitTcs = new();

        public int Id { get; } = id;
        public string FileName { get; } = fileName;
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
