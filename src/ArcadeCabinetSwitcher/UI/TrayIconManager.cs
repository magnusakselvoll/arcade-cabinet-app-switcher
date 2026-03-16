using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ArcadeCabinetSwitcher.UI;

public sealed class TrayIconManager : IDisposable
{
    private readonly TrayIcon _trayIcon;
    private readonly NativeMenu _menu;
    private IReadOnlyList<string> _profileNames = [];
    private string? _activeProfileName;

    public event EventHandler<string>? ProfileSwitchRequested;
    public event EventHandler? ExitRequested;

    public TrayIconManager(Application app)
    {
        _menu = new NativeMenu();

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Arcade Cabinet Switcher",
            Icon = CreateIcon(),
            Menu = _menu,
            IsVisible = true
        };

        TrayIcon.SetIcons(app, [_trayIcon]);

        RebuildMenu();
    }

    public void SetAvailableProfiles(IReadOnlyList<string> profileNames)
    {
        _profileNames = profileNames;
        RebuildMenu();
    }

    public void UpdateActiveProfile(string profileName)
    {
        _activeProfileName = profileName;
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        foreach (var name in _profileNames)
        {
            var capturedName = name;
            var item = new NativeMenuItem(name)
            {
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = string.Equals(name, _activeProfileName, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => ProfileSwitchRequested?.Invoke(this, capturedName);
            _menu.Items.Add(item);
        }

        if (_profileNames.Count > 0)
            _menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _menu.Items.Add(exitItem);
    }

    private static WindowIcon CreateIcon()
    {
        // 16×16 solid blue icon created programmatically — no external asset needed
        var bitmap = new WriteableBitmap(
            new PixelSize(16, 16),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using var frameBuffer = bitmap.Lock();
        var bytes = new byte[16 * 16 * 4];

        // Blue (BGRA premultiplied: B=0xF5, G=0x87, R=0x42, A=0xFF)
        for (var i = 0; i < 16 * 16; i++)
        {
            bytes[i * 4 + 0] = 0xF5; // B
            bytes[i * 4 + 1] = 0x87; // G
            bytes[i * 4 + 2] = 0x42; // R
            bytes[i * 4 + 3] = 0xFF; // A
        }

        Marshal.Copy(bytes, 0, frameBuffer.Address, bytes.Length);
        return new WindowIcon(bitmap);
    }

    public void Dispose() => _trayIcon.Dispose();
}
