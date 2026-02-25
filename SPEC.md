# Functional Specification

This document describes how the arcade cabinet app switcher should work. It serves as the authoritative source for functional requirements and design decisions.

## Overview

The Arcade Cabinet App Switcher is a Windows service that acts as an application launcher and switcher for a Windows-based arcade cabinet. When the machine powers on, Windows auto-logs in and the service starts automatically, launching the default profile. Users can switch between profiles using joystick button combinations on the arcade controls, without needing a keyboard or mouse.

## Terminology

| Term | Definition |
|------|------------|
| **Profile** | A named configuration consisting of one or more commands/programs to run, plus a joystick combo used to switch to it |
| **Default profile** | The profile launched automatically at service startup |
| **Switch combo** | A configurable combination of joystick buttons that, when held for a configured duration, triggers a switch to a specific profile |
| **Hold duration** | The number of seconds a switch combo must be held before the switch is triggered |
| **Active profile** | The profile whose processes are currently running |
| **Service** | The Windows Service that hosts the app switcher logic and runs in user context |

## Use Cases

### UC-1: System Startup

1. The Windows machine powers on and automatically logs in to a configured user account
2. The Windows Service starts in that user's context
3. The service loads the configuration file
4. The service launches the default profile's commands
5. The service begins monitoring for joystick input

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

### UC-6: Service Recovery

1. The service fails or crashes
2. Windows Service recovery policy restarts the service automatically (first and second failures)
3. On the third failure, the recovery policy reboots the machine
4. After each restart, the service resumes normal operation including launching the default profile

## Functional Requirements

### FR-1: Profiles and Configuration

- **FR-1.1** The service reads its configuration from a JSON settings file at a known location
- **FR-1.2** The configuration must specify exactly one default profile that is launched at startup
- **FR-1.3** Each profile has:
  - A unique name (string identifier)
  - One or more commands/programs to execute
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

- **FR-3.1** When launching a profile, the service starts each command defined in the profile as a child process
- **FR-3.2** The service tracks all directly launched processes and discovers their descendant sub-processes
- **FR-3.3** When terminating a profile, all tracked processes (root and sub-processes) are terminated
- **FR-3.4** Termination is attempted gracefully first (e.g., WM_CLOSE or equivalent); processes not exiting within a short timeout are forcefully killed
- **FR-3.5** The service waits for all processes to exit before launching the next profile

### FR-4: Windows Service

- **FR-4.1** The app switcher runs as a Windows Service
- **FR-4.2** The service runs in the logged-in user's context (not as SYSTEM or a dedicated service account) so that launched processes have access to the user's desktop session and environment
- **FR-4.3** The service is configured with a recovery policy:
  - First failure: restart the service
  - Second failure: restart the service
  - Third failure: reboot the machine
- **FR-4.4** The service starts automatically when Windows starts (after auto-login)

### FR-5: Installation

- **FR-5.1** The service is distributed as a Windows Installer package (MSI)
- **FR-5.2** The installer registers the service with Windows and configures the recovery policy
- **FR-5.3** The installer places the configuration file at the expected location with default/example content if no configuration exists
- **FR-5.4** The installer supports upgrading an existing installation without requiring manual uninstallation

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
        "C:\\Games\\MAME\\mame64.exe"
      ],
      "switchCombo": {
        "buttons": ["Button1", "Button2"],
        "holdDurationSeconds": 10
      }
    },
    {
      "name": "steam",
      "commands": [
        "C:\\Program Files (x86)\\Steam\\steam.exe -bigpicture"
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

- **Reliability**: The service must be stable under continuous operation. The Windows Service recovery policy acts as a safety net, but crashes should be minimised through robust error handling.
- **Platform**: Windows only. No cross-platform support is required or planned.
- **User context**: All launched applications must run in the interactive user session, not as background or system-level processes.
- **Low resource usage**: The service should have minimal CPU and memory overhead when idle (waiting for input or running a profile).
- **No UI**: The service itself has no user interface. All user interaction is via joystick input; all operator feedback is via logging.
