# Functional Specification

This document describes how the arcade cabinet app switcher should work. It serves as the authoritative source for functional requirements and design decisions.

## Overview

The Arcade Cabinet App Switcher is a startup application managed by Task Scheduler that acts as an application launcher and switcher for a Windows-based arcade cabinet. When the machine powers on, Windows auto-logs in and the application starts automatically, launching the default profile. Users can switch between profiles using joystick button combinations on the arcade controls, without needing a keyboard or mouse.

## Terminology

| Term | Definition |
|------|------------|
| **Profile** | A named configuration consisting of one or more commands/programs to run, plus a joystick combo used to switch to it |
| **Default profile** | The profile launched automatically at service startup |
| **Switch combo** | A configurable combination of joystick buttons that, when held for a configured duration, triggers a switch to a specific profile |
| **Hold duration** | The number of seconds a switch combo must be held before the switch is triggered |
| **Active profile** | The profile whose processes are currently running |
| **Application** | The startup application managed by Task Scheduler that hosts the app switcher logic and runs in the logged-in user's session |

## Use Cases

### UC-1: System Startup

1. The Windows machine powers on and automatically logs in to a configured user account
2. Task Scheduler starts the application in the logged-in user's context
3. The application loads the configuration file
4. The application launches the default profile's commands
5. The application begins monitoring for joystick input

### UC-2: Profile Switching

1. The user holds a configured joystick button combination for the required hold duration
2. The service detects the completed combo
3. The service terminates the active profile's processes (and their sub-processes) gracefully, then forcefully if needed
4. The service launches the selected profile's commands
5. The newly launched profile becomes the active profile

### UC-3: Special Profile — Reboot

1. The user triggers the switch combo for a reboot profile
2. The service terminates all active profile processes
3. The service initiates a system reboot

### UC-4: Special Profile — Shutdown

1. The user triggers the switch combo for a shutdown profile
2. The service terminates all active profile processes
3. The service initiates a system shutdown

### UC-5: Clean Process Termination

1. When closing a profile, the service identifies the root process(es) launched for that profile
2. The service discovers any child/sub-processes spawned by those root processes
3. The service first sends a graceful termination signal and waits briefly
4. Any processes that have not exited are forcefully terminated
5. The service confirms all processes are stopped before proceeding

### UC-6: Application Recovery

1. The application fails or crashes
2. Task Scheduler restart policy restarts the application automatically (up to 3 times, 5-second delay)
3. After each restart, the application resumes normal operation including launching the default profile

## Functional Requirements

### FR-1: Profiles and Configuration

- **FR-1.1** The service reads its configuration (`profiles.json`) from one of two locations, checked in order:
  1. `%AppData%\ArcadeCabinetSwitcher\profiles.json` — user override; takes priority if the file exists
  2. `<install directory>\profiles.json` — fallback default, placed there by the installer
- **FR-1.2** The configuration may optionally specify a default profile that is launched at startup; if omitted, the application starts without launching any profile and waits for input
- **FR-1.3** Each profile has:
  - A unique name (string identifier)
  - One or more commands/programs to execute; each command may optionally specify a `workingDirectory` and a `delaySeconds` (non-negative integer) to delay the launch of that command relative to the preceding one
  - A joystick switch combo (list of buttons) and hold duration in seconds
- **FR-1.4** Special profiles (reboot, shutdown) are supported via reserved command keywords or system actions rather than executable paths
- **FR-1.5** The configuration file must be validated on load; the service logs an error and fails to start if the configuration is invalid

### FR-2: Input Handling

- **FR-2.1** The service continuously monitors joystick/gamepad input while running
- **FR-2.2** Each profile defines its own distinct button combination for switching to it
- **FR-2.3** A switch is triggered only after the combo has been held for the configured hold duration (e.g., 10 seconds)
- **FR-2.4** Releasing the combo before the hold duration elapses cancels the switch
- **FR-2.5** Input monitoring runs independently of any launched profile processes

### FR-3: Process Management

- **FR-3.1** When launching a profile, the service starts each command defined in the profile as a child process. The working directory is set to the explicit `workingDirectory` if provided; otherwise it defaults to the directory containing the executable. If a command specifies `delaySeconds`, the service waits that many seconds before launching it.
- **FR-3.2** The service tracks all directly launched processes and discovers their descendant sub-processes
- **FR-3.3** When terminating a profile, all tracked processes (root and sub-processes) are terminated
- **FR-3.4** Termination is attempted gracefully first (e.g., WM_CLOSE or equivalent); processes not exiting within a short timeout are forcefully killed
- **FR-3.5** The service waits for all processes to exit before launching the next profile

### FR-4: Startup and Recovery

- **FR-4.1** The app switcher runs as a startup application managed by Task Scheduler
- **FR-4.2** The application runs in the logged-in user's interactive session so that launched processes have access to the user's desktop and environment
- **FR-4.3** The application is configured with a restart policy via Task Scheduler:
  - First failure: restart the application (5-second delay)
  - Second and third failure: restart the application (5-second delay)
  - Note: Task Scheduler does not support reboot-on-failure; automatic machine reboot on repeated failure is not supported
- **FR-4.4** The application starts automatically at user logon via Task Scheduler

### FR-5: Installation

- **FR-5.1** The service is distributed as a Windows Installer package (MSI)
- **FR-5.2** The installer registers a scheduled task with Windows and configures the restart policy
- **FR-5.3** The installer places the configuration file at the expected location with default/example content if no configuration exists
- **FR-5.4** The installer supports upgrading an existing installation without requiring manual uninstallation
- **FR-5.5** The installer recovers gracefully from interrupted installations — re-running the same version overwrites the previous installation regardless of its state
- **FR-5.6** The installer displays the locations of configuration files and log files in the finish dialog so operators know where to find them after installation

### FR-6: Updates

- **FR-6.1** The application supports an update mechanism to install newer versions on an already-installed machine
- **FR-6.2** Update details (mechanism and tooling) to be determined during implementation

### FR-7: Logging

- **FR-7.1** The service writes log entries to the Windows Event Log and/or a file-based log
- **FR-7.2** The following events are logged:
  - Service start and stop
  - Configuration loaded (and any validation errors)
  - Profile launched (name, commands)
  - Profile switch initiated and completed
  - Process termination (success and failures)
  - Input detection events (combo detected, hold progress)
  - Unhandled errors and exceptions

## Configuration Format

The configuration is stored as a JSON file. The following is an example showing the expected structure:

```json
{
  "defaultProfile": "mame",
  "profiles": [
    {
      "name": "mame",
      "commands": [
        { "command": "C:\\Games\\MAME\\mame64.exe", "workingDirectory": "C:\\Games\\MAME" }
      ],
      "switchCombo": {
        "buttons": ["Button1", "Button2"],
        "holdDurationSeconds": 10
      }
    },
    {
      "name": "steam",
      "commands": [
        { "command": "C:\\Program Files (x86)\\Steam\\steam.exe -bigpicture", "delaySeconds": 3 }
      ],
      "switchCombo": {
        "buttons": ["Button3", "Button4"],
        "holdDurationSeconds": 10
      }
    },
    {
      "name": "reboot",
      "action": "reboot",
      "switchCombo": {
        "buttons": ["Button1", "Button2", "Button3"],
        "holdDurationSeconds": 10
      }
    },
    {
      "name": "shutdown",
      "action": "shutdown",
      "switchCombo": {
        "buttons": ["Button1", "Button2", "Button3", "Button4"],
        "holdDurationSeconds": 10
      }
    }
  ]
}
```

> **Note:** The exact button names, configuration keys, and structure are subject to refinement during implementation.

## Non-Functional Requirements

- **Reliability**: The application must be stable under continuous operation. The Task Scheduler restart policy acts as a safety net, but crashes should be minimised through robust error handling.
- **Platform**: Windows only. No cross-platform support is required or planned.
- **User context**: All launched applications must run in the interactive user session, not as background or system-level processes.
- **Low resource usage**: The service should have minimal CPU and memory overhead when idle (waiting for input or running a profile).
- **No UI**: The service itself has no user interface. All user interaction is via joystick input; all operator feedback is via logging.
