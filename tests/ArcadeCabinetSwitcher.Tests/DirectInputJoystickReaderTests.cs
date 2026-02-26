using ArcadeCabinetSwitcher.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

/// <summary>
/// Integration tests for <see cref="SdlJoystickReader"/>.
/// Requires a physical joystick/gamepad connected to the machine.
/// Excluded from CI via <c>--filter "TestCategory!=Integration"</c>.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class DirectInputJoystickReaderTests
{
    [TestMethod]
    public void Initialize_WithJoystickConnected_ReturnsTrue()
    {
        using var reader = new SdlJoystickReader(NullLogger<SdlJoystickReader>.Instance);

        var result = reader.Initialize();

        Assert.IsTrue(result, "Expected Initialize to return true when a joystick is connected.");
    }

    [TestMethod]
    public void GetPressedButtons_ReturnsSet_WithoutThrowing()
    {
        using var reader = new SdlJoystickReader(NullLogger<SdlJoystickReader>.Instance);
        reader.Initialize();

        var pressed = reader.GetPressedButtons();

        Assert.IsNotNull(pressed);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        var reader = new SdlJoystickReader(NullLogger<SdlJoystickReader>.Instance);
        reader.Initialize();

        reader.Dispose();
        reader.Dispose();
    }

    [TestMethod]
    public void Initialize_WithNoJoysticksConnected_ReturnsFalse()
    {
        using var reader = new SdlJoystickReader(NullLogger<SdlJoystickReader>.Instance);

        var result = reader.Initialize();

        Assert.IsFalse(result, "Expected Initialize to return false when no joystick is connected.");
    }
}
