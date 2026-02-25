# Arcade Cabinet App Switcher

A Windows Service that acts as an application launcher and switcher for a Windows-based arcade cabinet. It starts automatically on boot, launches a default profile, and lets users switch between profiles using joystick button combinations — no keyboard or mouse required.

> **Status:** This project is currently in the specification phase. No implementation has begun yet. See [SPEC.md](SPEC.md) for the full functional specification.

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

<!-- TODO: Document prerequisites once the tech stack is implemented -->

## Quick Start

<!-- TODO: Add setup and run instructions once the project is set up -->

## Configuration

Configuration is stored in a JSON file. Each profile specifies the commands to run and the joystick combo used to switch to it. See [SPEC.md](SPEC.md) for the full configuration format and examples.

## License

This project is licensed under the terms of the [LICENSE](LICENSE) file.
