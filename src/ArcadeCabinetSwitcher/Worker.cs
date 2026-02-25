namespace ArcadeCabinetSwitcher;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Arcade Cabinet App Switcher starting");

        // TODO (#5): Load configuration via IConfigurationLoader
        // TODO (#6): Start input handler via IInputHandler.StartAsync
        // TODO (#7): Wire up profile switch requests to IProcessManager

        await Task.Delay(Timeout.Infinite, stoppingToken);

        logger.LogInformation("Arcade Cabinet App Switcher stopping");
    }
}
