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
- **Logging**: Serilog is used as the logging backend (infrastructure only — wired in `Program.cs` via `UseSerilog()`). All application code uses `Microsoft.Extensions.Logging.ILogger<T>`. Structured event IDs are defined in `src/ArcadeCabinetSwitcher/LogEvents.cs`. Console and Windows Event Log sinks are configured in `appsettings.json` under the `Serilog` key. The File sink is configured programmatically in `Program.cs` to write to `%LocalAppData%\ArcadeCabinetSwitcher\logs\arcade-cabinet-switcher.log` (absolute path, consistent across all run contexts). Serilog auto-creates the log directory.
- **Configuration loading**: Profile configuration is loaded from `profiles.json` (separate from `appsettings.json`) using `System.Text.Json`. `ConfigurationLoader` takes an optional `configFilePath` constructor parameter; when omitted it calls `ConfigurationPaths.ResolveProfilesConfigPath()` which checks `%AppData%\ArcadeCabinetSwitcher\profiles.json` first (user override, no admin required) and falls back to `AppContext.BaseDirectory/profiles.json` (install dir). `ConfigurationPaths` accepts optional `appDataDir`/`installDir` params for unit testing without environment manipulation. `ConfigurationValidator` is an `internal` static class with `InternalsVisibleTo` for the test project.
- **Optional default profile**: `AppSwitcherConfig.DefaultProfile` is `string?`. When null or absent, `Worker` logs at Information level (`NoDefaultProfile` event 3006) and skips the default launch — the application starts and monitors input without launching any profile. A non-empty value that does not match any profile name is still a validation error.
- **Optional switchCombo**: `ProfileConfig.SwitchCombo` is `SwitchComboConfig?`. Profiles without a switchCombo are excluded from joystick combo detection in `InputHandler` (filtered with `.Where(p => p.SwitchCombo is not null)`) but still appear in the overlay/tray UI. At least one profile must have a switchCombo — the validator enforces this. If a switchCombo IS provided, all existing validations still apply (non-empty buttons, positive hold duration).
- **Empty profiles**: A profile with neither `Commands` nor `Action` is valid. Switching to it terminates the active profile without launching anything — useful as an "idle" or "stop" profile. `ProcessManager.LaunchProfileAsync` already returns early for null/empty commands.
- **Polymorphic command format**: `ProfileConfig.Commands` is `IReadOnlyList<CommandConfig>?`. Each entry supports two JSON forms — a plain string (`"app.exe"`) or an object (`{ "command": "app.exe", "workingDirectory": "C:\\Dir", "delaySeconds": 3, "windowStyle": "hidden" }`). Deserialization is handled by `CommandConfigConverter : JsonConverter<CommandConfig>` (in `Configuration/CommandConfigConverter.cs`), applied via `[JsonConverter]` on `CommandConfig`. When `workingDirectory` is omitted, `ProcessManager` defaults to `Path.GetDirectoryName(fileName)`. When `delaySeconds` is set (must be >= 0), `ProcessManager.LaunchProfileAsync` awaits `Task.Delay` before launching that command, enabling sequenced startup (e.g. server before client). When `windowStyle` is set (valid values: `normal`, `hidden`, `minimized`, `maximized`; case-insensitive), `SystemProcessLauncher` sets `ProcessStartInfo.WindowStyle` accordingly; use `hidden` or `minimized` for background/server processes to prevent them stealing focus from a subsequently launched foreground application (the validator rejects any other string value).
- **MSTest assertions**: MSTest 4.x removed `Assert.ThrowsException<T>` and `[ExpectedException]`; use `Assert.ThrowsExactly<T>` instead.
- **WiX installer**: WiX v5 SDK-style project (`Sdk="WixToolset.Sdk/5.0.2"`). Not added to the `.slnx` — WiX only builds on Windows, whereas CI (`dotnet build`/`dotnet test`) runs on ubuntu-latest. Built explicitly in the release workflow only. `installer/Directory.Build.props` is intentionally empty to prevent the root `Directory.Build.props` (C# settings) from being applied to the WiX project. **Per-user install** (`Scope="perUser"`): installs to `%LocalAppData%\ArcadeCabinetSwitcher\` without requiring admin/UAC. All four deferred custom actions use `Impersonate="yes"` (runs as the installing user). ICE91 is suppressed in `SuppressIces` (expected warning for per-user directories). **ICE38/ICE64 fix**: per-user components must use an HKCU registry value as `KeyPath` instead of a file (ICE38 requirement); each component has `<RegistryValue Root="HKCU" Key="Software\ArcadeCabinetSwitcher" Name="<ComponentId>" Type="integer" Value="1" KeyPath="yes" />` and the `<File>` has no `KeyPath`. The `MainExecutable` component also has `<RemoveFolder Id="RemoveINSTALLFOLDER" On="uninstall" />` so the install directory is cleaned up on uninstall (ICE64 requirement). **WIX0230 / explicit GUIDs**: WiX v5 cannot auto-generate component GUIDs when a component contains both a registry KeyPath and a file — all components therefore carry an explicit `Guid="{...}"` attribute; never remove or replace these GUIDs as Windows Installer uses them to track component identity across upgrades. Resilience strategy: `AllowSameVersionUpgrades="yes"` makes re-running the same MSI trigger a full upgrade cycle instead of maintenance mode; `REINSTALLMODE=amus` forces file replacement regardless of version stamps (NeverOverwrite components are unaffected). UI provided by `WixToolset.UI.wixext` (`WixUI_Minimal`): shows progress with step messages and displays config/log file paths in the finish dialog. Task Scheduler management uses `WixQuietExec` from `WixToolset.Util.wixext` (`BinaryRef="Wix4UtilCA_X64"`): PowerShell `Register-ScheduledTask` creates the logon-trigger task with restart-on-failure settings (logon trigger, current user via `$env:USERNAME`, `LogonType Interactive`, RestartCount=3/1min, hidden, `-AllowStartIfOnBatteries` switch); `schtasks /Delete` removes it on uninstall; `taskkill` stops any running instance before upgrade/uninstall. The `Register-ScheduledTask` logic lives in `installer/ArcadeCabinetSwitcher.Installer/Create-ScheduledTask.ps1` (installed alongside the exe as the `ScheduledTaskScript` component) — the inline `-Command` approach was replaced with `-File` to stay within the 255-character MSI `CustomAction.Target` column limit (ICE03/WIX1076). **UpgradeCode** changed from the per-machine GUID to `{B3F2A104-E87D-4C59-9A16-5D0E7C8F3A21}` (Windows Installer cannot upgrade across install scopes; users with the old per-machine version must uninstall it manually first).
- **Native libraries**: The release workflow publishes with `IncludeNativeLibrariesForSelfExtract=false` so native DLLs are placed alongside the exe on disk (found via P/Invoke at runtime). Setting this to `true` embeds them inside the single-file exe, breaking runtime loading. Each native DLL must be explicitly listed as a component in `Package.wxs` — omitting one causes a `DllNotFoundException` crash on startup. Current native libraries: `SDL2.dll` (Silk.NET joystick input), `libSkiaSharp.dll` (Avalonia rendering backend), `libHarfBuzzSharp.dll` (Avalonia text rendering). When adding a new NuGet dependency that ships native binaries, check the win-x64 publish output and add any new DLLs to `Package.wxs`.
- **Button discovery**: When 2+ joystick buttons are held simultaneously for 10+ seconds, `InputHandler` logs at Information level with `profiles.json`-ready syntax (`ButtonDiscoveryLogged` event 5009). This is always-on (not gated by `InputDebugMode`) to help arcade operators identify button mappings without trial and error.
- **Descendant process tracking (FR-3.2)**: On Windows, `ProcessManager` uses Win32 Job Objects to track all descendant processes spawned by profile commands. A `JobObject` wrapper (P/Invoke via `LibraryImport` to `CreateJobObjectW`, `AssignProcessToJobObject`, `TerminateJobObject`, `SetInformationJobObject`) is created per profile launch. `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` is set as a safety net. During termination, `TerminateJobObject` kills all processes in the job — including orphaned children whose parent has already exited. The `IJobObjectFactory` interface returns `null` on non-Windows, falling back to individual `Kill(entireProcessTree: true)` calls. `kernel32.dll` is a Windows system library and does not need to be included in `Package.wxs`.

## Tech Stack

- **Language**: C#
- **Runtime**: .NET 10
- **Application type**: Generic Host console app (`WinExe` output type suppresses the window; launched at logon via Task Scheduler)

## Reference

- **SPEC.md**: Functional specification - describes how the application should work. Consult this for requirements and intended behavior.
