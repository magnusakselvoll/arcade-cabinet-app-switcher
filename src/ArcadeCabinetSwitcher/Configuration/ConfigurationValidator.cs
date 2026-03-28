namespace ArcadeCabinetSwitcher.Configuration;

internal static class ConfigurationValidator
{
    public static List<string> Validate(AppSwitcherConfig config)
    {
        var errors = new List<string>();

        if (config.Profiles.Count == 0)
        {
            errors.Add("Profiles list must not be empty.");
        }

        var profileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in config.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                errors.Add("Each profile must have a non-empty name.");
                continue;
            }

            if (!profileNames.Add(profile.Name))
            {
                errors.Add($"Duplicate profile name: '{profile.Name}' (names are case-insensitive).");
            }
        }

        if (!string.IsNullOrWhiteSpace(config.DefaultProfile) && !profileNames.Contains(config.DefaultProfile))
        {
            errors.Add($"DefaultProfile '{config.DefaultProfile}' does not match any profile name.");
        }

        foreach (var profile in config.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
                continue;

            var hasCommands = profile.Commands is { Count: > 0 };
            var hasAction = profile.Action.HasValue;

            if (hasCommands && hasAction)
            {
                errors.Add($"Profile '{profile.Name}': Cannot specify both Commands and Action.");
            }
            else if (hasCommands)
            {
                foreach (var cmd in profile.Commands!)
                {
                    if (string.IsNullOrWhiteSpace(cmd.Command))
                    {
                        errors.Add($"Profile '{profile.Name}': Commands must not contain blank entries.");
                        break;
                    }

                    if (cmd.WorkingDirectory is not null && string.IsNullOrWhiteSpace(cmd.WorkingDirectory))
                    {
                        errors.Add($"Profile '{profile.Name}': Command workingDirectory must not be blank.");
                        break;
                    }

                    if (cmd.DelaySeconds is < 0)
                    {
                        errors.Add($"Profile '{profile.Name}': Command delaySeconds must be >= 0.");
                        break;
                    }

                    if (cmd.WindowStyle is not null && !IsValidWindowStyle(cmd.WindowStyle))
                    {
                        errors.Add($"Profile '{profile.Name}': Command windowStyle '{cmd.WindowStyle}' is invalid. Valid values: normal, hidden, minimized, maximized.");
                        break;
                    }
                }
            }

            if (profile.SwitchCombo is not null)
            {
                if (profile.SwitchCombo.Buttons.Count == 0)
                {
                    errors.Add($"Profile '{profile.Name}': SwitchCombo.Buttons must not be empty.");
                }
                else
                {
                    foreach (var button in profile.SwitchCombo.Buttons)
                    {
                        if (string.IsNullOrWhiteSpace(button))
                        {
                            errors.Add($"Profile '{profile.Name}': SwitchCombo.Buttons must not contain blank entries.");
                            break;
                        }
                    }
                }

                if (profile.SwitchCombo.HoldDurationSeconds <= 0)
                {
                    errors.Add($"Profile '{profile.Name}': SwitchCombo.HoldDurationSeconds must be greater than 0.");
                }
            }
        }

        if (config.Profiles.Count > 0 && config.Profiles.All(p => p.SwitchCombo is null))
        {
            errors.Add("At least one profile must have a switchCombo so that profiles are accessible via joystick input.");
        }

        return errors;
    }

    private static bool IsValidWindowStyle(string value) =>
        value.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("hidden", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("minimized", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("maximized", StringComparison.OrdinalIgnoreCase);
}
