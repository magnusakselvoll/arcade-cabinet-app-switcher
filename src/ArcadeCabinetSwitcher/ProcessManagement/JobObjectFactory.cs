namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class JobObjectFactory : IJobObjectFactory
{
    public IJobObject? Create() =>
        OperatingSystem.IsWindows() ? new JobObject() : null;
}
