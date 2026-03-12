using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.ProcessManagement;

/// <summary>
/// Executes special system actions such as reboot and shutdown.
/// </summary>
public interface ISystemActionHandler
{
    /// <summary>
    /// Executes the given system action.
    /// </summary>
    Task ExecuteAsync(ProfileAction action, CancellationToken cancellationToken);
}
