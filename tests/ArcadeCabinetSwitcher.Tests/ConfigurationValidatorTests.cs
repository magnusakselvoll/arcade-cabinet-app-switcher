using ArcadeCabinetSwitcher.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class ConfigurationValidatorTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static ProfileConfig MakeProfile(
        string name = "app",
        string[]? commands = null,
        ProfileAction? action = null,
        string[]? buttons = null,
        int holdDurationSeconds = 5)
    {
        // Default to a valid command when neither commands nor action is specified,
        // so MakeProfile() (no args) produces a valid profile.
        var effectiveCommands = (commands is null && !action.HasValue) ? ["app.exe"] : commands;
        return new()
        {
            Name = name,
            Commands = effectiveCommands?.Select(c => new CommandConfig { Command = c }).ToArray(),
            Action = action,
            SwitchCombo = new SwitchComboConfig
            {
                Buttons = buttons ?? ["Button1"],
                HoldDurationSeconds = holdDurationSeconds
            }
        };
    }

    private static ProfileConfig MakeProfileWithCommandConfigs(
        string name,
        CommandConfig[] commandConfigs,
        string[]? buttons = null,
        int holdDurationSeconds = 5) =>
        new()
        {
            Name = name,
            Commands = commandConfigs,
            SwitchCombo = new SwitchComboConfig
            {
                Buttons = buttons ?? ["Button1"],
                HoldDurationSeconds = holdDurationSeconds
            }
        };

    private static AppSwitcherConfig MakeConfig(
        string? defaultProfile = "app",
        ProfileConfig[]? profiles = null)
    {
        var profileList = profiles ?? [MakeProfile()];
        return new AppSwitcherConfig
        {
            DefaultProfile = defaultProfile,
            Profiles = profileList
        };
    }

    // ── valid configurations ─────────────────────────────────────────────────

    [TestMethod]
    public void Validate_ValidCommandProfile_ReturnsNoErrors()
    {
        var errors = ConfigurationValidator.Validate(MakeConfig());
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_ValidActionProfile_ReturnsNoErrors()
    {
        var config = MakeConfig(
            defaultProfile: "reboot",
            profiles: [MakeProfile(name: "reboot", commands: null, action: ProfileAction.Reboot)]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_MultipleValidProfiles_ReturnsNoErrors()
    {
        var config = MakeConfig(
            defaultProfile: "mame",
            profiles:
            [
                MakeProfile("mame", commands: ["mame.exe"], buttons: ["B1", "B2"]),
                MakeProfile("steam", commands: ["steam.exe"], buttons: ["B3", "B4"]),
                MakeProfile("reboot", commands: null, action: ProfileAction.Reboot, buttons: ["B1", "B2", "B3"])
            ]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_DefaultProfileCaseInsensitive_ReturnsNoErrors()
    {
        var config = MakeConfig(defaultProfile: "APP", profiles: [MakeProfile("app")]);
        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    // ── empty / missing fields ───────────────────────────────────────────────

    [TestMethod]
    public void Validate_EmptyProfiles_ReturnsError()
    {
        var config = new AppSwitcherConfig { DefaultProfile = "app", Profiles = [] };
        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Count > 0);
        Assert.IsTrue(errors.Any(e => e.Contains("Profiles list")));
    }

    [TestMethod]
    public void Validate_NullDefaultProfile_ReturnsNoErrors()
    {
        var config = MakeConfig(defaultProfile: null);
        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_EmptyDefaultProfile_ReturnsNoErrors()
    {
        var config = MakeConfig(defaultProfile: "");
        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_DefaultProfileNotInProfiles_ReturnsError()
    {
        var config = MakeConfig(defaultProfile: "nonexistent");
        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("nonexistent") && e.Contains("does not match")));
    }

    [TestMethod]
    public void Validate_ProfileWithEmptyName_ReturnsError()
    {
        var config = MakeConfig(
            defaultProfile: "app",
            profiles: [MakeProfile("app"), MakeProfile("")]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("non-empty name")));
    }

    // ── duplicate profile names ──────────────────────────────────────────────

    [TestMethod]
    public void Validate_DuplicateProfileNames_ReturnsError()
    {
        var config = MakeConfig(
            profiles:
            [
                MakeProfile("app", buttons: ["B1"]),
                MakeProfile("app", buttons: ["B2"])
            ]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("Duplicate") && e.Contains("app")));
    }

    [TestMethod]
    public void Validate_DuplicateProfileNamesCaseInsensitive_ReturnsError()
    {
        var config = MakeConfig(
            defaultProfile: "App",
            profiles:
            [
                MakeProfile("App", buttons: ["B1"]),
                MakeProfile("APP", buttons: ["B2"])
            ]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("Duplicate")));
    }

    // ── Commands vs Action exclusivity ───────────────────────────────────────

    [TestMethod]
    public void Validate_BothCommandsAndAction_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", commands: ["cmd.exe"], action: ProfileAction.Reboot)]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("Cannot specify both")));
    }

    [TestMethod]
    public void Validate_NeitherCommandsNorAction_ReturnsNoErrors()
    {
        var profile = new ProfileConfig
        {
            Name = "app",
            Commands = null,
            Action = null,
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 5 }
        };
        var config = MakeConfig(profiles: [profile]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_EmptyCommandsList_ReturnsNoErrors()
    {
        var profile = new ProfileConfig
        {
            Name = "app",
            Commands = [],
            Action = null,
            SwitchCombo = new SwitchComboConfig { Buttons = ["B1"], HoldDurationSeconds = 5 }
        };
        var config = MakeConfig(profiles: [profile]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_CommandsWithBlankEntry_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", commands: ["valid.exe", "   "])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("blank entries")));
    }

    [TestMethod]
    public void Validate_CommandWithBlankWorkingDirectory_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", WorkingDirectory = "   " }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("workingDirectory")));
    }

    [TestMethod]
    public void Validate_CommandWithExplicitWorkingDirectory_ReturnsNoErrors()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", WorkingDirectory = "C:\\Games" }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    // ── SwitchCombo ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Validate_EmptyButtons_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", buttons: [])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("Buttons must not be empty")));
    }

    [TestMethod]
    public void Validate_ButtonsWithBlankEntry_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", buttons: ["Button1", ""])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("Buttons must not contain blank")));
    }

    [TestMethod]
    public void Validate_ZeroHoldDuration_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", holdDurationSeconds: 0)]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("HoldDurationSeconds")));
    }

    [TestMethod]
    public void Validate_NegativeHoldDuration_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfile("app", holdDurationSeconds: -1)]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("HoldDurationSeconds")));
    }

    // ── DelaySeconds ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Validate_CommandWithNullDelay_ReturnsNoErrors()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", DelaySeconds = null }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_CommandWithZeroDelay_ReturnsNoErrors()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", DelaySeconds = 0 }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_CommandWithPositiveDelay_ReturnsNoErrors()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", DelaySeconds = 5 }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_CommandWithNegativeDelay_ReturnsError()
    {
        var config = MakeConfig(
            profiles: [MakeProfileWithCommandConfigs("app",
                [new CommandConfig { Command = "app.exe", DelaySeconds = -1 }])]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("delaySeconds")));
    }

    // ── optional switchCombo ─────────────────────────────────────────────────

    [TestMethod]
    public void Validate_ProfileWithNullSwitchCombo_ReturnsNoErrors()
    {
        var profileWithCombo = MakeProfile("main");
        var profileWithoutCombo = new ProfileConfig
        {
            Name = "empty",
            Commands = null,
            Action = null,
            SwitchCombo = null
        };
        var config = MakeConfig(defaultProfile: null, profiles: [profileWithCombo, profileWithoutCombo]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_AllProfilesWithNullSwitchCombo_ReturnsError()
    {
        var config = new AppSwitcherConfig
        {
            DefaultProfile = null,
            Profiles =
            [
                new ProfileConfig { Name = "app", Commands = [new CommandConfig { Command = "app.exe" }], SwitchCombo = null }
            ]
        };

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Any(e => e.Contains("At least one profile")));
    }

    [TestMethod]
    public void Validate_EmptyProfileNoSwitchCombo_ReturnsNoErrors()
    {
        // A profile with no commands, no action, and no switchCombo is valid
        // as long as at least one other profile has a switchCombo.
        var mainProfile = MakeProfile("main");
        var emptyProfile = new ProfileConfig { Name = "idle", Commands = null, Action = null, SwitchCombo = null };
        var config = MakeConfig(defaultProfile: null, profiles: [mainProfile, emptyProfile]);

        var errors = ConfigurationValidator.Validate(config);
        Assert.AreEqual(0, errors.Count);
    }

    // ── multiple errors ──────────────────────────────────────────────────────

    [TestMethod]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var config = new AppSwitcherConfig
        {
            DefaultProfile = null,
            Profiles =
            [
                new ProfileConfig
                {
                    Name = "app",
                    Commands = null,
                    Action = null,
                    SwitchCombo = new SwitchComboConfig { Buttons = [], HoldDurationSeconds = 0 }
                }
            ]
        };

        var errors = ConfigurationValidator.Validate(config);
        Assert.IsTrue(errors.Count >= 2, $"Expected at least 2 errors but got {errors.Count}: {string.Join("; ", errors)}");
    }
}
