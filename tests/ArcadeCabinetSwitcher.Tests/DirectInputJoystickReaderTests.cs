using ArcadeCabinetSwitcher.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

/// <summary>
/// Integration tests for <see cref="DirectInputJoystickReader"/>.
/// Requires a physical joystick/gamepad connected via DirectInput (Windows only).
/// Excluded from CI via <c>--filter "TestCategory!=Integration"</c>.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class DirectInputJoystickReaderTests
{
    [TestMethod]
    public void Initialize_WithJoystickConnected_ReturnsTrue()
    {
        using var reader = new DirectInputJoystickReader(NullLogger<DirectInputJoystickReader>.Instance);

        var result = reader.Initialize();

        Assert.IsTrue(result, "Expected Initialize to return true when a joystick is connected.");
    }

    [TestMethod]
    public void GetPressedButtons_ReturnsSet_WithoutThrowing()
    {
        using var reader = new DirectInputJoystickReader(NullLogger<DirectInputJoystickReader>.Instance);
        reader.Initialize();

        var pressed = reader.GetPressedButtons();

        Assert.IsNotNull(pressed);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        var reader = new DirectInputJoystickReader(NullLogger<DirectInputJoystickReader>.Instance);
        reader.Initialize();

        reader.Dispose();
        reader.Dispose(); // Second dispose should not throw
    }

    [TestMethod]
    public void Initialize_OnNonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows())
            Assert.Inconclusive("This test only applies on non-Windows platforms.");

        using var reader = new DirectInputJoystickReader(NullLogger<DirectInputJoystickReader>.Instance);

        var result = reader.Initialize();

        Assert.IsFalse(result);
    }
}
