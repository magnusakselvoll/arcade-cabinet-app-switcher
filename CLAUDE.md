# CLAUDE.md

Instructions for Claude Code when working on this repository.

## Issue Tracking

Issues are tracked in GitHub. Use `gh issue list` to see open issues and `gh issue view <number>` for details.

## Git Workflow (GitHub Flow)

Always use GitHub Flow when working on issues:

1. **Create a feature branch** from `main` before starting work:
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
- **Logging**: Serilog is used as the logging backend (infrastructure only — wired in `Program.cs` via `UseSerilog()`). All application code uses `Microsoft.Extensions.Logging.ILogger<T>`. Structured event IDs are defined in `src/ArcadeCabinetSwitcher/LogEvents.cs`. Console and Windows Event Log sinks are configured in `appsettings.json` under the `Serilog` key. The File sink is configured programmatically in `Program.cs` to write to `%ProgramData%\ArcadeCabinetSwitcher\logs\arcade-cabinet-switcher.log` (absolute path, consistent across all run contexts). Serilog auto-creates the log directory.
- **Configuration loading**: Profile configuration is loaded from `profiles.json` (separate from `appsettings.json`) using `System.Text.Json`. `ConfigurationLoader` takes an optional `configFilePath` constructor parameter (defaults to `AppContext.BaseDirectory/profiles.json`) to enable testing without mocking. `ConfigurationValidator` is an `internal` static class with `InternalsVisibleTo` for the test project.
- **MSTest assertions**: MSTest 4.x removed `Assert.ThrowsException<T>` and `[ExpectedException]`; use `Assert.ThrowsExactly<T>` instead.
- **WiX installer**: WiX v5 SDK-style project (`Sdk="WixToolset.Sdk/5.0.2"`). Not added to the `.slnx` — WiX only builds on Windows, whereas CI (`dotnet build`/`dotnet test`) runs on ubuntu-latest. Built explicitly in the release workflow only. `installer/Directory.Build.props` is intentionally empty to prevent the root `Directory.Build.props` (C# settings) from being applied to the WiX project. Resilience strategy: `AllowSameVersionUpgrades="yes"` makes re-running the same MSI trigger a full upgrade cycle instead of maintenance mode; `REINSTALLMODE=amus` forces file replacement regardless of version stamps (NeverOverwrite components are unaffected). UI provided by `WixToolset.UI.wixext` (`WixUI_Minimal`): shows progress with step messages and displays config/log file paths in the finish dialog. Task Scheduler management uses `WixQuietExec` from `WixToolset.Util.wixext` (`BinaryRef="Wix4UtilCA_X64"`): PowerShell `Register-ScheduledTask` creates the logon-trigger task with restart-on-failure settings; `schtasks /Delete` removes it on uninstall; `taskkill` stops any running instance before upgrade/uninstall.
- **SDL2 / native library**: The release workflow publishes with `IncludeNativeLibrariesForSelfExtract=false` so SDL2.dll is placed alongside the exe on disk (Silk.NET finds it via P/Invoke). Setting this to `true` embeds SDL2.dll inside the single-file exe, which breaks runtime loading. SDL2.dll is explicitly listed as a `Sdl2Native` component in `Package.wxs`, so it is included in the MSI alongside the executable.
- **Button discovery**: When 2+ joystick buttons are held simultaneously for 10+ seconds, `InputHandler` logs at Information level with `profiles.json`-ready syntax (`ButtonDiscoveryLogged` event 5009). This is always-on (not gated by `InputDebugMode`) to help arcade operators identify button mappings without trial and error.

## Tech Stack

- **Language**: C#
- **Runtime**: .NET 10
- **Application type**: Generic Host console app (`WinExe` output type suppresses the window; launched at logon via Task Scheduler)

## Reference

- **SPEC.md**: Functional specification - describes how the application should work. Consult this for requirements and intended behavior.
