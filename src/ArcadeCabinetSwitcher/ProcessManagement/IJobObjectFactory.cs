namespace ArcadeCabinetSwitcher.ProcessManagement;

internal interface IJobObjectFactory
{
    IJobObject? Create();
}
