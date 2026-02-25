using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.ProcessManagement;

/// <summary>
/// Manages the lifecycle of profile processes — launching, tracking, and termination.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Launches all commands defined in the given profile and tracks the resulting processes.
    /// </summary>
    Task LaunchProfileAsync(ProfileConfig profile, CancellationToken cancellationToken);

    /// <summary>
    /// Terminates all processes belonging to the currently active profile.
    /// Attempts graceful termination first; forcefully kills any remaining processes.
    /// </summary>
    Task TerminateActiveProfileAsync(CancellationToken cancellationToken);
}
