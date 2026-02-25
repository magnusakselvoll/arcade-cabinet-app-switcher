using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher;

public class Worker(ILogger<Worker> logger, IConfigurationLoader configurationLoader) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(LogEvents.ServiceStarting, "Arcade Cabinet App Switcher starting");

        var config = configurationLoader.Load();

        // TODO (#6): Start input handler via IInputHandler.StartAsync
        // TODO (#7): Wire up profile switch requests to IProcessManager (using config)

        await Task.Delay(Timeout.Infinite, stoppingToken);

        logger.LogInformation(LogEvents.ServiceStopping, "Arcade Cabinet App Switcher stopping");
    }
}
