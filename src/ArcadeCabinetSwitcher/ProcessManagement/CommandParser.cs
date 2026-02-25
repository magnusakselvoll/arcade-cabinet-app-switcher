namespace ArcadeCabinetSwitcher.ProcessManagement;

internal static class CommandParser
{
    /// <summary>
    /// Parses a command string into a (FileName, Arguments) tuple.
    /// Supports quoted file names, e.g. <c>"C:\Program Files\app.exe" -bigpicture</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
    public static (string FileName, string Arguments) Parse(string command)
    {
        if (string.IsNullOrEmpty(command))
            throw new ArgumentException("Command must not be null or empty.", nameof(command));

        if (command.StartsWith('"'))
        {
            var closingQuote = command.IndexOf('"', 1);
            if (closingQuote < 0)
                return (command[1..], string.Empty);

            var fileName = command[1..closingQuote];
            var remainder = command[(closingQuote + 1)..].TrimStart();
            return (fileName, remainder);
        }

        var spaceIndex = command.IndexOf(' ');
        if (spaceIndex < 0)
            return (command, string.Empty);

        return (command[..spaceIndex], command[(spaceIndex + 1)..].TrimStart());
    }
}
