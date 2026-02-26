namespace ArcadeCabinetSwitcher.Input;

/// <summary>
/// Abstracts hardware joystick access for testability.
/// </summary>
internal interface IJoystickReader : IDisposable
{
    /// <summary>
    /// Acquires available joystick devices. Returns <c>false</c> if no devices are found.
    /// </summary>
    bool Initialize();

    /// <summary>
    /// Returns the set of button names currently pressed across all acquired devices.
    /// </summary>
    IReadOnlySet<string> GetPressedButtons();
}
