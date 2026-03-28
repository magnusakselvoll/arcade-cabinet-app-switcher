# Arcade Cabinet App Switcher

A startup application that acts as an application launcher and switcher for a Windows-based arcade cabinet. It starts automatically at user logon via Task Scheduler, launches a default profile, and lets users switch between profiles using joystick button combinations — no keyboard or mouse required.

> **Status:** Early development. The core project scaffold is in place; full functionality is under active development. See [SPEC.md](SPEC.md) for the full functional specification.

## Features

- **Auto-launch on startup** — Starts with Windows (after auto-login) and immediately launches the default profile
- **Profile-based launching** — Each profile defines one or more programs/commands to run
- **Joystick-driven switching** — Switch profiles by holding a configurable button combo for a set duration (e.g., 10 seconds)
- **Clean process termination** — Tracks child/sub-processes to ensure everything is properly closed when switching profiles
- **Special profiles** — Built-in support for reboot and shutdown profiles
- **Reliable** — Automatic restart on failure via Task Scheduler; starts automatically at user logon
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

# Run locally
dotnet run --project src/ArcadeCabinetSwitcher
```

## Configuration

Profile configuration is stored in `profiles.json`. The service checks two locations in order and uses the first file it finds:

1. **`%AppData%\ArcadeCabinetSwitcher\profiles.json`** — user override; takes priority if present. No admin rights required to edit.
2. **`<install directory>\profiles.json`** — default, placed there by the installer (typically `%LocalAppData%\ArcadeCabinetSwitcher\`). This location is user-writable, so no admin rights are required to edit it.

On first install, the default `profiles.json` is pre-populated with example content. You can edit it directly in the install directory, or copy it to `%AppData%\ArcadeCabinetSwitcher\` if you prefer to keep your config separate from the install directory.

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

Commands can also be specified as objects to control additional options:

| Property | Required | Description |
|---|---|---|
| `command` | Yes | The executable path and optional arguments |
| `workingDirectory` | No | Working directory for the process (defaults to the executable's directory) |
| `delaySeconds` | No | Seconds to wait before launching this command (useful for sequencing) |
| `windowStyle` | No | Initial window state: `normal`, `hidden`, `minimized`, or `maximized`. Use `hidden` or `minimized` for background/server processes to prevent them from stealing focus from a subsequently launched UI application. |

```json
{
  "name": "photobooth",
  "commands": [
    {
      "command": "C:\\PhotoBooth\\PhotoBooth.Server.exe",
      "workingDirectory": "C:\\PhotoBooth",
      "windowStyle": "hidden"
    },
    {
      "command": "\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\" --kiosk http://localhost:5000 --edge-kiosk-type=fullscreen",
      "delaySeconds": 3
    }
  ],
  "switchCombo": { "buttons": ["Button1", "Button2"], "holdDurationSeconds": 10 }
}
```

See [SPEC.md](SPEC.md) for the full configuration format and validation rules.

## Installation (Windows)

Download the `.msi` from the [Releases](https://github.com/magnusakselvoll/arcade-cabinet-app-switcher/releases) page and run it. **No admin rights required** — the installer runs without a UAC prompt. The installer:

- Copies files to `%LocalAppData%\ArcadeCabinetSwitcher\`
- Registers the `ArcadeCabinetSwitcher` Task Scheduler task for the current user (logon trigger)
- Configures the restart policy automatically: restarts up to 3 times on failure (5-second delay)
- Preserves existing `appsettings.json` and `profiles.json` when upgrading

> **Upgrading from an older per-machine version?** If you previously installed a version that used `C:\Program Files\ArcadeCabinetSwitcher\`, uninstall it first (via Programs & Features or `msiexec /x`). The new per-user installer uses a different install scope and cannot auto-upgrade the old version.

After installation, the application starts immediately. Edit `profiles.json` (in `%AppData%\ArcadeCabinetSwitcher\` for a no-admin option, or in the install directory) to configure your profiles, then restart the task (`schtasks /End /TN ArcadeCabinetSwitcher` and `schtasks /Run /TN ArcadeCabinetSwitcher`).

## Logging

By default, events are written to the **console** and to a **rolling daily log file** at:

```
%LocalAppData%\ArcadeCabinetSwitcher\logs\arcade-cabinet-switcher.log
```

The directory is created automatically on first run.

### Enable Windows Event Log

To also write to the Windows Event Log, first create the event source once (run as Administrator):

```powershell
New-EventLog -LogName Application -Source "ArcadeCabinetSwitcher"
```

Then uncomment the `EventLog` sink block in `appsettings.json`.

### Change the minimum log level

Edit the `MinimumLevel.Default` value in `appsettings.json` (e.g., `"Debug"` for verbose output).

## Button Discovery

Not sure which button names to use in `profiles.json`? The service has built-in button discovery:

1. Start the service with your joystick connected
2. Hold any combination of **2 or more buttons** for **10 seconds**
3. Check the log file — the service will log a ready-to-use `profiles.json` snippet:

```
Buttons held for 10+ seconds: Button1, Button3. Use in profiles.json: "buttons": ["Button1", "Button3"], "holdDurationSeconds": 10
```

No debug mode or configuration change required.

## License

This project is licensed under the terms of the [LICENSE](LICENSE) file.
