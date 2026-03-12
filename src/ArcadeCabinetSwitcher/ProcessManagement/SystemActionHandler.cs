using ArcadeCabinetSwitcher.Configuration;

namespace ArcadeCabinetSwitcher.ProcessManagement;

public sealed class SystemActionHandler(ILogger<SystemActionHandler> logger) : ISystemActionHandler
{
    public Task ExecuteAsync(ProfileAction action, CancellationToken cancellationToken)
    {
        var (command, args) = action switch
        {
            ProfileAction.Reboot => ("shutdown", "/r /t 0"),
            ProfileAction.Shutdown => ("shutdown", "/s /t 0"),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        logger.LogInformation(LogEvents.SpecialActionExecuted, "Executing system action: {Action}", action);

        System.Diagnostics.Process.Start(command, args);
        return Task.CompletedTask;
    }
}
