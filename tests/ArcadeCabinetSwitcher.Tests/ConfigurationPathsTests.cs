using ArcadeCabinetSwitcher.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class ConfigurationPathsTests
{
    private string? _tempAppDataDir;
    private string? _tempInstallDir;

    [TestInitialize]
    public void Setup()
    {
        _tempAppDataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempInstallDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempAppDataDir);
        Directory.CreateDirectory(_tempInstallDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempAppDataDir is not null && Directory.Exists(_tempAppDataDir))
            Directory.Delete(_tempAppDataDir, recursive: true);
        if (_tempInstallDir is not null && Directory.Exists(_tempInstallDir))
            Directory.Delete(_tempInstallDir, recursive: true);
    }

    [TestMethod]
    public void ResolveProfilesConfigPath_AppDataFileExists_ReturnsAppDataPath()
    {
        var appDataProfilesJson = Path.Combine(_tempAppDataDir!, "profiles.json");
        File.WriteAllText(appDataProfilesJson, "{}");

        var resolved = ConfigurationPaths.ResolveProfilesConfigPath(_tempAppDataDir, _tempInstallDir);

        Assert.AreEqual(appDataProfilesJson, resolved);
    }

    [TestMethod]
    public void ResolveProfilesConfigPath_AppDataFileDoesNotExist_ReturnsInstallDirPath()
    {
        // No profiles.json in appDataDir

        var resolved = ConfigurationPaths.ResolveProfilesConfigPath(_tempAppDataDir, _tempInstallDir);

        var expected = Path.Combine(_tempInstallDir!, "profiles.json");
        Assert.AreEqual(expected, resolved);
    }

    [TestMethod]
    public void ResolveProfilesConfigPath_BothFilesExist_ReturnsAppDataPath()
    {
        var appDataProfilesJson = Path.Combine(_tempAppDataDir!, "profiles.json");
        var installProfilesJson = Path.Combine(_tempInstallDir!, "profiles.json");
        File.WriteAllText(appDataProfilesJson, "{}");
        File.WriteAllText(installProfilesJson, "{}");

        var resolved = ConfigurationPaths.ResolveProfilesConfigPath(_tempAppDataDir, _tempInstallDir);

        Assert.AreEqual(appDataProfilesJson, resolved);
    }
}
