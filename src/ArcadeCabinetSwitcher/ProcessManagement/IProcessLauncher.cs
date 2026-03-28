using System.Diagnostics;

namespace ArcadeCabinetSwitcher.ProcessManagement;

internal interface IProcessLauncher
{
    IProcessHandle Start(string fileName, string arguments, string? workingDirectory, ProcessWindowStyle? windowStyle = null);
}
