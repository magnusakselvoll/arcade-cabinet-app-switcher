# CLAUDE.md

Instructions for Claude Code when working on this repository.

## Issue Tracking

Issues are tracked in GitHub. Use `gh issue list` to see open issues and `gh issue view <number>` for details.

## Git Workflow (GitHub Flow)

Always use GitHub Flow when working on issues:

1. **Create a feature branch** from `main` before starting work — do not read or edit any files until the branch is created:
   - First fetch and checkout latest main: `git fetch origin && git checkout main && git pull`
   - Branch name format: `<issue-number>-<short-description>` (e.g., `6-enhance-look-and-feel`)
   - Example: `git checkout -b 6-enhance-look-and-feel`

2. **Commit** changes with descriptive messages

3. **Push** the branch and **create a PR**:
   - **Ask before creating the PR** - the user may have feedback based on the console output or code
   - PR title should be descriptive of the change
   - Reference the issue in the PR body with `Closes #<issue-number>` to auto-close on merge
   - Use `gh pr create` for convenience

4. **Merge** after review (squash merge preferred for clean history)

5. **Clean up** after the user confirms a PR is merged:
   - `git fetch origin && git checkout main && git pull`
   - `git branch -d <branch-name>`

## Documentation Updates

When closing issues via PR, consider updating:
- **SPEC.md** - Functional requirements, use cases, expected behavior
- **README.md** - Setup instructions, configuration, user-facing changes
- **CLAUDE.md** - Technical implementation details, architecture, known issues, build commands

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run locally (console app — WinExe suppresses the window only on Windows)
dotnet run --project src/ArcadeCabinetSwitcher

# Build MSI installer (Windows only; publish win-x64 first)
dotnet build installer/ArcadeCabinetSwitcher.Installer/ArcadeCabinetSwitcher.Installer.wixproj ^
  -p:InstallerVersion=1.0.0 ^
  "-p:PublishDir=%CD%\publish\win-x64\"
```

## Architecture

```
arcade-cabinet-app-switcher/
├── ArcadeCabinetSwitcher.slnx          # Solution file (.slnx format)
├── Directory.Build.props               # Shared: nullable, implicit usings, warnings-as-errors
├── Directory.Packages.props            # Central Package Management — all NuGet versions here
├── src/
│   └── ArcadeCabinetSwitcher/
│       ├── Program.cs                  # Host builder — Generic Host console app, registers Worker and ConfigurationLoader
│       ├── Worker.cs                   # BackgroundService entry point — loads config on startup
│       ├── profiles.json               # Default/example profile configuration (copied to output dir)
│       ├── Configuration/              # Config POCOs, IConfigurationLoader, ConfigurationLoader, ConfigurationValidator
│       ├── Input/                      # IInputHandler — joystick monitoring
│       └── ProcessManagement/          # IProcessManager — process lifecycle
├── installer/
│   ├── Directory.Build.props           # Empty — stops root Directory.Build.props from applying to WiX project
│   └── ArcadeCabinetSwitcher.Installer/
│       ├── ArcadeCabinetSwitcher.Installer.wixproj  # WiX v5 SDK-style project (NOT in .slnx — Windows-only)
│       └── Package.wxs                 # MSI definition: Task Scheduler task, restart policy, config preservation
└── tests/
    └── ArcadeCabinetSwitcher.Tests/    # MSTest test project
```

Key decisions:
- **TFM**: `net10.0` (not `net10.0-windows`) — buildable on macOS; switch to `-windows` when Windows APIs are needed
- **Central Package Management**: all NuGet package versions are pinned in `Directory.Packages.props`
- **Shared MSBuild settings**: `Directory.Build.props` sets nullable, implicit usings, and warnings-as-errors for all projects
- **Logging**: Serilog backend wired in `Program.cs` via `UseSerilog()`; app code uses `ILogger<T>`. Event IDs in `LogEvents.cs`. File sink path configured programmatically in `Program.cs` (`%LocalAppData%\ArcadeCabinetSwitcher\logs\arcade-cabinet-switcher.log`); Console and Windows Event Log sinks in `appsettings.json`.
- **Configuration loading**: `ConfigurationLoader` / `ConfigurationPaths` / `ConfigurationValidator` (internal, `InternalsVisibleTo` for test project). `ConfigurationPaths` accepts optional `appDataDir`/`installDir` params for unit testing without environment manipulation.
- **Polymorphic command format**: `ProfileConfig.Commands` is `IReadOnlyList<CommandConfig>?`. Each entry is a plain string or an object (`{ "command": "...", "workingDirectory": "...", "delaySeconds": 3, "windowStyle": "hidden" }`). Deserialization via `CommandConfigConverter : JsonConverter<CommandConfig>` in `Configuration/CommandConfigConverter.cs`.
- **MSTest assertions**: MSTest 4.x removed `Assert.ThrowsException<T>` and `[ExpectedException]`; use `Assert.ThrowsExactly<T>` instead.
- **WiX installer**: Per-user (`Scope="perUser"`, `%LocalAppData%`). ICE38/ICE64: components use an HKCU registry value as `KeyPath` (not the file). WIX0230: all components carry explicit `Guid="{...}"` — never change these, Windows Installer uses them for upgrade tracking. `Create-ScheduledTask.ps1` used via `-File` (not inline `-Command`) to stay within the 255-char `CustomAction.Target` limit (ICE03/WIX1076). `UpgradeCode` is `{B3F2A104-E87D-4C59-9A16-5D0E7C8F3A21}` — changed from per-machine GUID; users with old per-machine installs must uninstall manually first.
- **Native libraries**: Published with `IncludeNativeLibrariesForSelfExtract=false` — native DLLs go alongside the exe (setting to `true` embeds them in the single-file exe, breaking P/Invoke). Each DLL must be a component in `Package.wxs` or it causes a `DllNotFoundException` on startup. Current: `SDL2.dll`, `libSkiaSharp.dll`, `libHarfBuzzSharp.dll`.
- **Descendant process tracking**: `JobObject` wrapper (P/Invoke via `LibraryImport` to `kernel32.dll`) created per profile launch; `TerminateJobObject` kills all descendants including orphaned children. `IJobObjectFactory` returns `null` on non-Windows, falling back to `Kill(entireProcessTree: true)`.

## Tech Stack

- **Language**: C#
- **Runtime**: .NET 10
- **Application type**: Generic Host console app (`WinExe` output type suppresses the window; launched at logon via Task Scheduler)

## Reference

- **SPEC.md**: Functional specification - describes how the application should work. Consult this for requirements and intended behavior.
