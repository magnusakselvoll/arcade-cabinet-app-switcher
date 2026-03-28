using System.Diagnostics;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal sealed class SystemProcessLauncher : IProcessLauncher
{
    public IProcessHandle Start(string fileName, string arguments, string? workingDirectory, ProcessWindowStyle? windowStyle = null)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory ?? string.Empty,
            WindowStyle = windowStyle ?? ProcessWindowStyle.Normal
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Process.Start returned null for '{fileName}'.");

        return new SystemProcessHandle(process);
    }
}
