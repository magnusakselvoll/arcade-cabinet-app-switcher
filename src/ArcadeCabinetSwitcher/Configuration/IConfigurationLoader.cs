namespace ArcadeCabinetSwitcher.Configuration;

/// <summary>
/// Loads and validates the app switcher configuration from disk.
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads the configuration file and returns the validated configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the configuration is missing or invalid.</exception>
    AppSwitcherConfig Load();
}
