using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class InputHandlerTests
{
    private static AppSwitcherConfig CreateConfig(params (string Name, string[] Buttons, int HoldSeconds)[] profiles)
    {
        return new AppSwitcherConfig
        {
            DefaultProfile = profiles[0].Name,
            Profiles = profiles.Select(p => new ProfileConfig
            {
                Name = p.Name,
                Commands = ["notepad.exe"],
                SwitchCombo = new SwitchComboConfig
                {
                    Buttons = p.Buttons,
                    HoldDurationSeconds = p.HoldSeconds
                }
            }).ToArray()
        };
    }

    [TestMethod]
    public async Task ComboHeldForFullDuration_FiresProfileSwitchRequested()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);
        stub.SetState(["Button1"]);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        var eventFired = new TaskCompletionSource<string>();
        handler.ProfileSwitchRequested += (_, name) => eventFired.TrySetResult(name);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        // Allow first poll iteration to record the start timestamp
        await Task.Delay(20);

        // Advance fake time past hold duration
        fakeTime.Advance(TimeSpan.FromSeconds(3));

        var result = await eventFired.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("Profile1", result);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ComboReleasedBeforeDuration_DoesNotFireEvent()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        string? firedProfile = null;
        handler.ProfileSwitchRequested += (_, name) => firedProfile = name;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        // Hold the button briefly
        stub.SetState(["Button1"]);
        await Task.Delay(20);

        // Release before hold duration (no fake time advancement)
        stub.SetState([]);
        await Task.Delay(20);

        Assert.IsNull(firedProfile);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task PartialCombo_DoesNotFireEvent()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);
        stub.SetState(["Button1"]); // Only one of the two required buttons

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1", "Button2"], 3));

        string? firedProfile = null;
        handler.ProfileSwitchRequested += (_, name) => firedProfile = name;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        // Advance time well past hold duration
        await Task.Delay(20);
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(20);

        Assert.IsNull(firedProfile);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task MultipleProfiles_CorrectProfileSelected()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);
        stub.SetState(["Button3", "Button4"]); // Matches Profile2

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(
            ("Profile1", ["Button1", "Button2"], 3),
            ("Profile2", ["Button3", "Button4"], 3));

        var eventFired = new TaskCompletionSource<string>();
        handler.ProfileSwitchRequested += (_, name) => eventFired.TrySetResult(name);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        await Task.Delay(20);
        fakeTime.Advance(TimeSpan.FromSeconds(3));

        var result = await eventFired.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("Profile2", result);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ComboReleasedAndRepressed_TimerResets()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);
        stub.SetState(["Button1"]);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        var eventFired = new TaskCompletionSource<string>();
        handler.ProfileSwitchRequested += (_, name) => eventFired.TrySetResult(name);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        // Let combo become active and advance time — but NOT past hold duration
        await Task.Delay(20);
        fakeTime.Advance(TimeSpan.FromSeconds(2));
        await Task.Delay(10);

        // Release combo: timer should reset
        stub.SetState([]);
        await Task.Delay(20);

        // Re-press combo: timer starts fresh
        stub.SetState(["Button1"]);
        await Task.Delay(20);

        // Now advance past hold duration from the re-press
        fakeTime.Advance(TimeSpan.FromSeconds(3));

        var result = await eventFired.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("Profile1", result);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ExtraButtonsPressed_DoNotPreventComboMatch()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);
        stub.SetState(["Button1", "Button2", "Button99"]); // Extra button present

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1", "Button2"], 3));

        var eventFired = new TaskCompletionSource<string>();
        handler.ProfileSwitchRequested += (_, name) => eventFired.TrySetResult(name);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        await Task.Delay(20);
        fakeTime.Advance(TimeSpan.FromSeconds(3));

        var result = await eventFired.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("Profile1", result);

        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task NoJoystickAvailable_LogsWarningAndContinuesWithoutCrash()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: false);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Should not throw
        await handler.StartAsync(config, cts.Token);
        await Task.Delay(20);
        await handler.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task StopAsync_PollingLoopExitsCleanly()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), false);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        await Task.Delay(20);

        // StopAsync should complete without hanging
        await handler.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task DebugMode_LogsMultiButtonPresses()
    {
        var fakeTime = new FakeTimeProvider();
        var stub = new StubJoystickReader(initialized: true);

        // Start with no buttons, then press two
        stub.SetState([]);

        var handler = new InputHandler(
            NullLogger<InputHandler>.Instance, stub, fakeTime, TimeSpan.FromMilliseconds(1), debugMode: true);
        var config = CreateConfig(("Profile1", ["Button1"], 3));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await handler.StartAsync(config, cts.Token);

        await Task.Delay(10);

        // Press two buttons — handler should log them (debug mode)
        stub.SetState(["Button5", "Button6"]);
        await Task.Delay(20);

        // No crash; test passes if we get here
        await handler.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Stub joystick reader that returns a controllable set of pressed buttons.
    /// </summary>
    private sealed class StubJoystickReader : IJoystickReader
    {
        private readonly bool _initializeResult;
        private volatile IReadOnlySet<string> _currentState = new HashSet<string>();

        public StubJoystickReader(bool initialized = true)
        {
            _initializeResult = initialized;
        }

        public void SetState(IEnumerable<string> buttons)
        {
            _currentState = new HashSet<string>(buttons, StringComparer.OrdinalIgnoreCase);
        }

        public bool Initialize() => _initializeResult;

        public IReadOnlySet<string> GetPressedButtons() => _currentState;

        public void Dispose() { }
    }
}
