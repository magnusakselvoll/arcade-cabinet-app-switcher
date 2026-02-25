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

# Run the service locally (gracefully degrades on non-Windows)
dotnet run --project src/ArcadeCabinetSwitcher
```

## Architecture

```
arcade-cabinet-app-switcher/
├── ArcadeCabinetSwitcher.slnx          # Solution file (.slnx format)
├── Directory.Build.props               # Shared: nullable, implicit usings, warnings-as-errors
├── Directory.Packages.props            # Central Package Management — all NuGet versions here
├── src/
│   └── ArcadeCabinetSwitcher/
│       ├── Program.cs                  # Host builder — AddWindowsService(), registers Worker
│       ├── Worker.cs                   # BackgroundService entry point
│       ├── Configuration/              # Config POCOs (AppSwitcherConfig) and IConfigurationLoader
│       ├── Input/                      # IInputHandler — joystick monitoring
│       └── ProcessManagement/          # IProcessManager — process lifecycle
└── tests/
    └── ArcadeCabinetSwitcher.Tests/    # xUnit test project
```

Key decisions:
- **TFM**: `net10.0` (not `net10.0-windows`) — buildable on macOS; switch to `-windows` when Windows APIs are needed
- **Central Package Management**: all NuGet package versions are pinned in `Directory.Packages.props`
- **Shared MSBuild settings**: `Directory.Build.props` sets nullable, implicit usings, and warnings-as-errors for all projects

## Tech Stack

- **Language**: C#
- **Runtime**: .NET 10
- **Application type**: Windows Service (Generic Host with `UseWindowsService()`)

## Reference

- **SPEC.md**: Functional specification - describes how the application should work. Consult this for requirements and intended behavior.
