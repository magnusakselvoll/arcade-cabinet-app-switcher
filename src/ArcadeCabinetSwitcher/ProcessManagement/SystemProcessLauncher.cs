using System.Diagnostics;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class SystemProcessLauncher : IProcessLauncher
{
    public IProcessHandle Start(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Process.Start returned null for '{fileName}'.");

        return new SystemProcessHandle(process);
    }
}
