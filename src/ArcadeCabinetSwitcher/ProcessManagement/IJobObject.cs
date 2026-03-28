namespace ArcadeCabinetSwitcher.ProcessManagement;

internal interface IJobObject : IDisposable
{
    bool TryAssignProcess(nint processHandle);
    void Terminate(uint exitCode = 1);
}
