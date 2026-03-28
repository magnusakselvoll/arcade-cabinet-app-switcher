using System.Diagnostics;
using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly IProcessLauncher _processLauncher;
    private readonly IJobObjectFactory? _jobObjectFactory;
    private readonly TimeSpan _gracefulExitTimeout;
    private readonly Lock _lock = new();
    private readonly List<IProcessHandle> _activeProcesses = [];
    private IJobObject? _activeJobObject;

    public ProcessManager(ILogger<ProcessManager> logger, IProcessLauncher processLauncher, IJobObjectFactory? jobObjectFactory)
        : this(logger, processLauncher, jobObjectFactory, TimeSpan.FromSeconds(5)) { }

    internal ProcessManager(ILogger<ProcessManager> logger, IProcessLauncher processLauncher, IJobObjectFactory? jobObjectFactory, TimeSpan gracefulExitTimeout)
    {
        _logger = logger;
        _processLauncher = processLauncher;
        _jobObjectFactory = jobObjectFactory;
        _gracefulExitTimeout = gracefulExitTimeout;
    }

    public async Task LaunchProfileAsync(ProfileConfig profile, CancellationToken cancellationToken)
    {
        if (profile.Commands is null || profile.Commands.Count == 0)
            return;

        _activeJobObject = CreateJobObject();

        foreach (var commandConfig in profile.Commands)
        {
            if (commandConfig.DelaySeconds is > 0)
                await Task.Delay(TimeSpan.FromSeconds(commandConfig.DelaySeconds.Value), cancellationToken);

            try
            {
                var (fileName, arguments) = CommandParser.Parse(commandConfig.Command);
                var workingDirectory = commandConfig.WorkingDirectory ?? Path.GetDirectoryName(fileName) ?? string.Empty;
                var windowStyle = ParseWindowStyle(commandConfig.WindowStyle);
                var handle = _processLauncher.Start(fileName, arguments, workingDirectory, windowStyle);

                lock (_lock)
                {
                    _activeProcesses.Add(handle);
                }

                _logger.LogInformation(LogEvents.ProcessLaunched,
                    "Launched process {ProcessId} for command {Command} with working directory {WorkingDirectory}",
                    handle.Id, commandConfig.Command, workingDirectory);

                TryAssignToJobObject(handle);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.ProcessLaunchFailed, ex,
                    "Failed to launch command {Command}", commandConfig.Command);
            }
        }

        _logger.LogInformation(LogEvents.ProfileLaunched,
            "Launched profile {ProfileName} with {CommandCount} command(s)",
            profile.Name, profile.Commands.Count);
    }

    public async Task TerminateActiveProfileAsync(CancellationToken cancellationToken)
    {
        List<IProcessHandle> snapshot;
        IJobObject? jobObject;
        lock (_lock)
        {
            snapshot = [.._activeProcesses];
            _activeProcesses.Clear();
            jobObject = _activeJobObject;
            _activeJobObject = null;
        }

        if (snapshot.Count == 0 && jobObject is null)
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
            // Kill all processes in the job object — this terminates root processes and any
            // orphaned child processes whose parent has already exited (the FR-3.2 fix)
            if (jobObject is not null)
            {
                jobObject.Terminate();
                _logger.LogInformation(LogEvents.JobObjectTerminated,
                    "Terminated job object (all descendant processes)");
                jobObject.Dispose();
            }

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

    private IJobObject? CreateJobObject()
    {
        if (_jobObjectFactory is null)
            return null;

        try
        {
            var job = _jobObjectFactory.Create();
            if (job is not null)
                _logger.LogInformation(LogEvents.JobObjectCreated, "Created job object for profile");
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(LogEvents.JobObjectCreateFailed, ex,
                "Failed to create job object — child process tracking will be limited");
            return null;
        }
    }

    private void TryAssignToJobObject(IProcessHandle handle)
    {
        if (_activeJobObject is null)
            return;

        try
        {
            if (!_activeJobObject.TryAssignProcess(handle.NativeHandle))
            {
                _logger.LogWarning(LogEvents.ProcessAssignToJobFailed,
                    "Failed to assign process {ProcessId} to job object", handle.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(LogEvents.ProcessAssignToJobFailed, ex,
                "Failed to assign process {ProcessId} to job object", handle.Id);
        }
    }

    private static ProcessWindowStyle? ParseWindowStyle(string? value) =>
        value?.ToLowerInvariant() switch
        {
            null => null,
            "normal" => ProcessWindowStyle.Normal,
            "hidden" => ProcessWindowStyle.Hidden,
            "minimized" => ProcessWindowStyle.Minimized,
            "maximized" => ProcessWindowStyle.Maximized,
            _ => null
        };
}
