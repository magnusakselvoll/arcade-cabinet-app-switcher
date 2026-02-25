using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly IProcessLauncher _processLauncher;
    private readonly TimeSpan _gracefulExitTimeout;
    private readonly Lock _lock = new();
    private readonly List<IProcessHandle> _activeProcesses = [];

    public ProcessManager(ILogger<ProcessManager> logger, IProcessLauncher processLauncher)
        : this(logger, processLauncher, TimeSpan.FromSeconds(5)) { }

    internal ProcessManager(ILogger<ProcessManager> logger, IProcessLauncher processLauncher, TimeSpan gracefulExitTimeout)
    {
        _logger = logger;
        _processLauncher = processLauncher;
        _gracefulExitTimeout = gracefulExitTimeout;
    }

    public Task LaunchProfileAsync(ProfileConfig profile, CancellationToken cancellationToken)
    {
        if (profile.Commands is null || profile.Commands.Count == 0)
            return Task.CompletedTask;

        foreach (var command in profile.Commands)
        {
            try
            {
                var (fileName, arguments) = CommandParser.Parse(command);
                var handle = _processLauncher.Start(fileName, arguments);

                lock (_lock)
                {
                    _activeProcesses.Add(handle);
                }

                _logger.LogInformation(LogEvents.ProcessLaunched,
                    "Launched process {ProcessId} for command {Command}", handle.Id, command);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.ProcessLaunchFailed, ex,
                    "Failed to launch command {Command}", command);
            }
        }

        _logger.LogInformation(LogEvents.ProfileLaunched,
            "Launched profile {ProfileName} with {CommandCount} command(s)",
            profile.Name, profile.Commands.Count);

        return Task.CompletedTask;
    }

    public async Task TerminateActiveProfileAsync(CancellationToken cancellationToken)
    {
        List<IProcessHandle> snapshot;
        lock (_lock)
        {
            snapshot = [.._activeProcesses];
            _activeProcesses.Clear();
        }

        if (snapshot.Count == 0)
            return;

        _logger.LogInformation(LogEvents.ProfileTerminationStarted,
            "Terminating {ProcessCount} active process(es)", snapshot.Count);

        var toKill = new List<IProcessHandle>();

        foreach (var handle in snapshot)
        {
            if (handle.HasExited)
            {
                _logger.LogInformation(LogEvents.ProcessTerminated,
                    "Process {ProcessId} already exited", handle.Id);
                handle.Dispose();
                continue;
            }

            handle.CloseMainWindow();
            toKill.Add(handle);
        }

        try
        {
            if (toKill.Count > 0)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_gracefulExitTimeout);

                try
                {
                    await Task.WhenAll(toKill.Select(h => h.WaitForExitAsync(timeoutCts.Token)));
                }
                catch (OperationCanceledException)
                {
                    // Graceful timeout elapsed or service shutdown — proceed to force-kill
                }
            }
        }
        finally
        {
            foreach (var handle in toKill)
            {
                try
                {
                    if (!handle.HasExited)
                    {
                        handle.Kill(entireProcessTree: true);
                        _logger.LogInformation(LogEvents.ProcessTerminated,
                            "Force-killed process {ProcessId}", handle.Id);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process exited between HasExited check and Kill() — treat as success
                    _logger.LogInformation(LogEvents.ProcessTerminated,
                        "Process {ProcessId} exited before force-kill", handle.Id);
                }
                finally
                {
                    handle.Dispose();
                }
            }
        }

        _logger.LogInformation(LogEvents.ProfileTerminationCompleted, "Profile termination completed");
    }
}
