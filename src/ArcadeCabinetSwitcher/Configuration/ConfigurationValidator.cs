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

        if (string.IsNullOrWhiteSpace(config.DefaultProfile))
        {
            errors.Add("DefaultProfile must not be empty.");
        }
        else if (!profileNames.Contains(config.DefaultProfile))
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
            else if (!hasCommands && !hasAction)
            {
                errors.Add($"Profile '{profile.Name}': Must specify either Commands or Action.");
            }
            else if (hasCommands)
            {
                foreach (var cmd in profile.Commands!)
                {
                    if (string.IsNullOrWhiteSpace(cmd))
                    {
                        errors.Add($"Profile '{profile.Name}': Commands must not contain blank entries.");
                        break;
                    }
                }
            }

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

        return errors;
    }
}
