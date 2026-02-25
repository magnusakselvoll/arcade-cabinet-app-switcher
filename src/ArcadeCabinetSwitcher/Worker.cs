using ArcadeCabinetSwitcher.Configuration;
using ArcadeCabinetSwitcher.Input;

namespace ArcadeCabinetSwitcher;

public class Worker(
    ILogger<Worker> logger,
    IConfigurationLoader configurationLoader,
    IInputHandler inputHandler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(LogEvents.ServiceStarting, "Arcade Cabinet App Switcher starting");

        var config = configurationLoader.Load();

        inputHandler.ProfileSwitchRequested += (_, profileName) =>
            logger.LogInformation(LogEvents.ProfileSwitchStarted,
                "Profile switch requested: {ProfileName}", profileName);

        await inputHandler.StartAsync(config, stoppingToken);

        // TODO (#8): Wire up profile switch requests to IProcessManager (using config)

        await Task.Delay(Timeout.Infinite, stoppingToken);

        logger.LogInformation(LogEvents.ServiceStopping, "Arcade Cabinet App Switcher stopping");
    }
}
