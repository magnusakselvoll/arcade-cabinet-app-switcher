using ArcadeCabinetSwitcher.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class ConfigurationLoaderTests
{
    private string? _tempFile;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempFile is not null && File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    private string WriteTempJson(string json)
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        File.WriteAllText(_tempFile, json);
        return _tempFile;
    }

    private static ConfigurationLoader MakeLoader(string path) =>
        new(NullLogger<ConfigurationLoader>.Instance, path);

    // ── valid load ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Load_ValidConfig_ReturnsConfig()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "mame",
              "profiles": [
                {
                  "name": "mame",
                  "commands": ["mame.exe"],
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();

        Assert.AreEqual("mame", config.DefaultProfile);
        Assert.AreEqual(1, config.Profiles.Count);
        Assert.AreEqual("mame", config.Profiles[0].Name);
        Assert.AreEqual(1, config.Profiles[0].Commands!.Count);
        Assert.AreEqual("mame.exe", config.Profiles[0].Commands![0]);
    }

    [TestMethod]
    public void Load_ValidConfig_ReturnsCorrectProfileCount()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "mame",
              "profiles": [
                {
                  "name": "mame",
                  "commands": ["mame.exe"],
                  "switchCombo": { "buttons": ["B1", "B2"], "holdDurationSeconds": 10 }
                },
                {
                  "name": "reboot",
                  "action": "reboot",
                  "switchCombo": { "buttons": ["B1", "B2", "B3"], "holdDurationSeconds": 10 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual(2, config.Profiles.Count);
    }

    // ── missing file ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Load_MissingFile_ThrowsInvalidOperationException()
    {
        var loader = MakeLoader(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json"));
        Assert.ThrowsExactly<InvalidOperationException>(() => loader.Load());
    }

    // ── malformed JSON ───────────────────────────────────────────────────────

    [TestMethod]
    public void Load_MalformedJson_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("{ this is not valid json }");
        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    [TestMethod]
    public void Load_EmptyFile_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("");
        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    // ── missing required fields ──────────────────────────────────────────────

    [TestMethod]
    public void Load_MissingDefaultProfile_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("""
            {
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe"],
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    [TestMethod]
    public void Load_MissingProfiles_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("""{ "defaultProfile": "app" }""");
        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    // ── validation errors ────────────────────────────────────────────────────

    [TestMethod]
    public void Load_DefaultProfileNotInProfiles_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "nonexistent",
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe"],
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    [TestMethod]
    public void Load_DuplicateProfileNames_ThrowsInvalidOperationException()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "app",
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe"],
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                },
                {
                  "name": "app",
                  "commands": ["app2.exe"],
                  "switchCombo": { "buttons": ["B2"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        Assert.ThrowsExactly<InvalidOperationException>(() => MakeLoader(path).Load());
    }

    // ── camelCase mapping ────────────────────────────────────────────────────

    [TestMethod]
    public void Load_CamelCaseProperties_MapsCorrectly()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "app",
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe"],
                  "switchCombo": { "buttons": ["Button1"], "holdDurationSeconds": 7 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual(7, config.Profiles[0].SwitchCombo.HoldDurationSeconds);
        Assert.AreEqual("Button1", config.Profiles[0].SwitchCombo.Buttons[0]);
    }

    // ── enum deserialization ─────────────────────────────────────────────────

    [TestMethod]
    public void Load_RebootAction_DeserializesEnum()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "reboot",
              "profiles": [
                {
                  "name": "reboot",
                  "action": "reboot",
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual(ProfileAction.Reboot, config.Profiles[0].Action);
    }

    [TestMethod]
    public void Load_ShutdownAction_DeserializesEnum()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "shutdown",
              "profiles": [
                {
                  "name": "shutdown",
                  "action": "shutdown",
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual(ProfileAction.Shutdown, config.Profiles[0].Action);
    }

    // ── tolerance features ───────────────────────────────────────────────────

    [TestMethod]
    public void Load_JsonWithComments_Succeeds()
    {
        var path = WriteTempJson("""
            {
              // This is a comment
              "defaultProfile": "app",
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe"],
                  "switchCombo": { "buttons": ["B1"], "holdDurationSeconds": 5 }
                }
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual("app", config.DefaultProfile);
    }

    [TestMethod]
    public void Load_JsonWithTrailingCommas_Succeeds()
    {
        var path = WriteTempJson("""
            {
              "defaultProfile": "app",
              "profiles": [
                {
                  "name": "app",
                  "commands": ["app.exe",],
                  "switchCombo": { "buttons": ["B1",], "holdDurationSeconds": 5 }
                },
              ]
            }
            """);

        var config = MakeLoader(path).Load();
        Assert.AreEqual("app", config.DefaultProfile);
    }
}
