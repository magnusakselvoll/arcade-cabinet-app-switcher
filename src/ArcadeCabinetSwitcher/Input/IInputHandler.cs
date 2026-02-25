using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.Input;

/// <summary>
/// Monitors joystick input and raises an event when a profile switch combo is completed.
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Raised when a switch combo has been held for the required duration.
    /// The event argument is the name of the profile to switch to.
    /// </summary>
    event EventHandler<string> ProfileSwitchRequested;

    /// <summary>
    /// Begins monitoring joystick input using the provided configuration.
    /// </summary>
    Task StartAsync(AppSwitcherConfig config, CancellationToken cancellationToken);

    /// <summary>
    /// Stops monitoring joystick input.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
