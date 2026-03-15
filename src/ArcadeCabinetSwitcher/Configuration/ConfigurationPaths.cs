namespace ArcadeCabinetSwitcher.Configuration;

internal static class ConfigurationPaths
{
    private const string AppName = "ArcadeCabinetSwitcher";
    private const string FileName = "profiles.json";

    /// <summary>
    /// Resolves the path to profiles.json, preferring the AppData override location
    /// over the install directory fallback.
    /// </summary>
    /// <param name="appDataDir">Override for %AppData%\ArcadeCabinetSwitcher (for testing).</param>
    /// <param name="installDir">Override for the install/base directory (for testing).</param>
    /// <returns>The AppData path if it exists; otherwise the install directory path.</returns>
    internal static string ResolveProfilesConfigPath(string? appDataDir = null, string? installDir = null)
    {
        appDataDir ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);
        installDir ??= AppContext.BaseDirectory;

        var appDataPath = Path.Combine(appDataDir, FileName);
        if (File.Exists(appDataPath))
            return appDataPath;

        return Path.Combine(installDir, FileName);
    }
}
