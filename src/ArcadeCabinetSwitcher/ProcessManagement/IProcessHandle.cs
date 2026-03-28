namespace ArcadeCabinetSwitcher.ProcessManagement;

internal interface IProcessHandle : IDisposable
{
    int Id { get; }
    nint NativeHandle { get; }
    bool HasExited { get; }
    bool CloseMainWindow();
    Task WaitForExitAsync(CancellationToken cancellationToken);
    void Kill(bool entireProcessTree);
}
