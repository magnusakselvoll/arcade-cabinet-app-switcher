using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;
using ArcadeCabinetSwitcher.ProcessManagement;
using ArcadeCabinetSwitcher.UI;

namespace ArcadeCabinetSwitcher;

public class Worker(
    ILogger<Worker> logger,
    IConfigurationLoader configurationLoader,
    IInputHandler inputHandler,
    IProcessManager processManager,
    ISystemActionHandler systemActionHandler,
    IOverlayService overlayService) : BackgroundService
{
    private string? _activeProfileName;
    private int _switching; // 0 = idle, 1 = in progress

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(LogEvents.ServiceStarting, "Arcade Cabinet App Switcher starting");

        var config = configurationLoader.Load();

        overlayService.SetAvailableProfiles(config.Profiles.Select(p => p.Name).ToList());

        inputHandler.ProfileSwitchRequested += (_, profileName) =>
            OnProfileSwitchRequested(profileName, config, stoppingToken);

        overlayService.ProfileSwitchRequested += (_, profileName) =>
            OnProfileSwitchRequested(profileName, config, stoppingToken);

        await inputHandler.StartAsync(config, stoppingToken);

        if (string.IsNullOrWhiteSpace(config.DefaultProfile))
        {
            logger.LogInformation(LogEvents.NoDefaultProfile,
                "No default profile configured; waiting for input");
        }
        else
        {
            var defaultProfile = config.Profiles.FirstOrDefault(p =>
                string.Equals(p.Name, config.DefaultProfile, StringComparison.OrdinalIgnoreCase));

            if (defaultProfile is not null)
            {
                logger.LogInformation(LogEvents.DefaultProfileLaunched,
                    "Launching default profile: {ProfileName}", defaultProfile.Name);
                await processManager.LaunchProfileAsync(defaultProfile, stoppingToken);
                _activeProfileName = defaultProfile.Name;
                overlayService.ShowProfileNotification(defaultProfile.Name);
                overlayService.UpdateActiveProfile(defaultProfile.Name);
            }
            else
            {
                logger.LogWarning(LogEvents.ProfileSwitchFailed,
                    "Default profile '{ProfileName}' not found in configuration", config.DefaultProfile);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(LogEvents.ServiceStopping, "Arcade Cabinet App Switcher stopping");
        await inputHandler.StopAsync(cancellationToken);
        await processManager.TerminateActiveProfileAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async void OnProfileSwitchRequested(string profileName, AppSwitcherConfig config, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _switching, 1, 0) != 0)
        {
            logger.LogInformation(LogEvents.ProfileSwitchIgnored,
                "Profile switch to '{ProfileName}' ignored — switch already in progress", profileName);
            return;
        }

        try
        {
            var target = config.Profiles.FirstOrDefault(p =>
                string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                logger.LogError(LogEvents.ProfileSwitchFailed,
                    "Profile switch requested for unknown profile '{ProfileName}'", profileName);
                return;
            }

            logger.LogInformation(LogEvents.ProfileSwitchStarted,
                "Switching to profile: {ProfileName}", profileName);

            await processManager.TerminateActiveProfileAsync(cancellationToken);

            if (target.Action is { } action)
            {
                await systemActionHandler.ExecuteAsync(action, cancellationToken);
            }
            else
            {
                await processManager.LaunchProfileAsync(target, cancellationToken);
            }

            _activeProfileName = target.Name;
            overlayService.ShowProfileNotification(target.Name);
            overlayService.UpdateActiveProfile(target.Name);

            logger.LogInformation(LogEvents.ProfileSwitchCompleted,
                "Profile switch to '{ProfileName}' completed", profileName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(LogEvents.ProfileSwitchFailed, ex,
                "Profile switch to '{ProfileName}' failed", profileName);
        }
        finally
        {
            Interlocked.Exchange(ref _switching, 0);
        }
    }
}
