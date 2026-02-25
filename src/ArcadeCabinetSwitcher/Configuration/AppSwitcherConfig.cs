namespace ArcadeCabinetSwitcher.Configuration;

/// <summary>
/// Root configuration model. Matches the JSON structure defined in SPEC.md.
/// </summary>
public sealed class AppSwitcherConfig
{
    public required string DefaultProfile { get; init; }
    public required IReadOnlyList<ProfileConfig> Profiles { get; init; }
}

/// <summary>
/// Configuration for a single profile. Either <see cref="Commands"/> or <see cref="Action"/> must be set.
/// </summary>
public sealed class ProfileConfig
{
    public required string Name { get; init; }

    /// <summary>
    /// Command strings to execute for this profile. Mutually exclusive with <see cref="Action"/>.
    /// </summary>
    public IReadOnlyList<string>? Commands { get; init; }

    /// <summary>
    /// Special system action for this profile. Mutually exclusive with <see cref="Commands"/>.
    /// </summary>
    public ProfileAction? Action { get; init; }

    public required SwitchComboConfig SwitchCombo { get; init; }
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
