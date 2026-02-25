# Arcade Cabinet App Switcher

A Windows Service that acts as an application launcher and switcher for a Windows-based arcade cabinet. It starts automatically on boot, launches a default profile, and lets users switch between profiles using joystick button combinations — no keyboard or mouse required.

> **Status:** Early development. The core project scaffold is in place; full functionality is under active development. See [SPEC.md](SPEC.md) for the full functional specification.

## Features

- **Auto-launch on startup** — Starts with Windows (after auto-login) and immediately launches the default profile
- **Profile-based launching** — Each profile defines one or more programs/commands to run
- **Joystick-driven switching** — Switch profiles by holding a configurable button combo for a set duration (e.g., 10 seconds)
- **Clean process termination** — Tracks child/sub-processes to ensure everything is properly closed when switching profiles
- **Special profiles** — Built-in support for reboot and shutdown profiles
- **Reliable service** — Windows Service with recovery policy: restart on first/second failure, reboot on third
- **JSON configuration** — All profiles and combos are defined in a simple JSON settings file
- **Windows Installer (MSI)** — Packaged for easy installation and upgrade
- **Logging** — Service events, profile switches, and errors written to Windows Event Log and/or file

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Quick Start

```bash
# Clone the repo
git clone https://github.com/magnusakselvoll/arcade-cabinet-app-switcher.git
cd arcade-cabinet-app-switcher

# Build
dotnet build

# Run tests
dotnet test

# Run locally (console mode — UseWindowsService() degrades gracefully on non-Windows)
dotnet run --project src/ArcadeCabinetSwitcher
```

## Configuration

Profile configuration is stored in `profiles.json`, located in the same directory as the service executable. On first install the file is pre-populated with an example configuration that you can edit.

Each profile specifies either the commands to run or a special action (`reboot`/`shutdown`), along with the joystick combo used to switch to it:

```json
{
  "defaultProfile": "mame",
  "profiles": [
    {
      "name": "mame",
      "commands": ["C:\\Games\\MAME\\mame64.exe"],
      "switchCombo": { "buttons": ["Button1", "Button2"], "holdDurationSeconds": 10 }
    },
    {
      "name": "reboot",
      "action": "reboot",
      "switchCombo": { "buttons": ["Button1", "Button2", "Button3"], "holdDurationSeconds": 10 }
    }
  ]
}
```

See [SPEC.md](SPEC.md) for the full configuration format and validation rules.

## Logging

Logging is configured in `appsettings.json` under the `Serilog` key. By default, events are written to the **console** and to a **rolling daily log file** under `logs/`.

### Enable Windows Event Log

To also write to the Windows Event Log, first create the event source once (run as Administrator):

```powershell
New-EventLog -LogName Application -Source "ArcadeCabinetSwitcher"
```

Then uncomment the `EventLog` sink block in `appsettings.json`.

### Change the minimum log level

Edit the `MinimumLevel.Default` value in `appsettings.json` (e.g., `"Debug"` for verbose output).

## License

This project is licensed under the terms of the [LICENSE](LICENSE) file.
