using System.Diagnostics;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class SystemProcessHandle(Process process) : IProcessHandle
{
    public int Id => process.Id;
    public bool HasExited => process.HasExited;

    public bool CloseMainWindow() => process.CloseMainWindow();

    public Task WaitForExitAsync(CancellationToken cancellationToken) =>
        process.WaitForExitAsync(cancellationToken);

    public void Kill(bool entireProcessTree) => process.Kill(entireProcessTree);

    public void Dispose() => process.Dispose();
}
