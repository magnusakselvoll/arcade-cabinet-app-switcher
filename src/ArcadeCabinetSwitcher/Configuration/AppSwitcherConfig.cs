using System.Text.Json.Serialization;

namespace ArcadeCabinetSwitcher.Configuration;

/// <summary>
/// Root configuration model. Matches the JSON structure defined in SPEC.md.
/// </summary>
public sealed class AppSwitcherConfig
{
    public string? DefaultProfile { get; init; }
    public required IReadOnlyList<ProfileConfig> Profiles { get; init; }
}

/// <summary>
/// Configuration for a single profile. Either <see cref="Commands"/> or <see cref="Action"/> must be set.
/// </summary>
public sealed class ProfileConfig
{
    public required string Name { get; init; }

    /// <summary>
    /// Commands to execute for this profile. Each entry may be a plain string or an object with
    /// <c>command</c> and optional <c>workingDirectory</c>. Mutually exclusive with <see cref="Action"/>.
    /// </summary>
    public IReadOnlyList<CommandConfig>? Commands { get; init; }

    /// <summary>
    /// Special system action for this profile. Mutually exclusive with <see cref="Commands"/>.
    /// </summary>
    public ProfileAction? Action { get; init; }

    public SwitchComboConfig? SwitchCombo { get; init; }
}

/// <summary>
/// A single command entry. Supports both plain-string and object forms in JSON via
/// <see cref="CommandConfigConverter"/>:
/// <code>
/// "app.exe"
/// { "command": "app.exe", "workingDirectory": "C:\\Games" }
/// </code>
/// When <see cref="WorkingDirectory"/> is omitted, the process is started in the directory
/// containing the executable.
/// </summary>
[JsonConverter(typeof(CommandConfigConverter))]
public sealed class CommandConfig
{
    public required string Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public int? DelaySeconds { get; init; }
}

/// <summary>
/// Special system actions a profile can trigger instead of launching commands.
/// </summary>
public enum ProfileAction
{
    Reboot,
    Shutdown,
}

/// <summary>
/// Joystick button combination and hold duration that triggers a profile switch.
/// </summary>
public sealed class SwitchComboConfig
{
    public required IReadOnlyList<string> Buttons { get; init; }
    public required int HoldDurationSeconds { get; init; }
}
