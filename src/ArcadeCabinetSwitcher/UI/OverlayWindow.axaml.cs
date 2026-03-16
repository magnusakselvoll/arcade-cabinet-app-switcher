using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArcadeCabinetSwitcher.UI;

public partial class OverlayWindow : Window
{
    private DispatcherTimer? _hideTimer;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void ShowNotification(string profileName)
    {
        ProfileText.Text = $"Switching to: {profileName}";
        IsVisible = true;

        // Position after layout pass so Bounds are accurate
        Dispatcher.UIThread.Post(PositionAtBottomCenter, DispatcherPriority.Render);

        _hideTimer?.Stop();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideTimer.Tick += (_, _) =>
        {
            IsVisible = false;
            _hideTimer!.Stop();
        };
        _hideTimer.Start();
    }

    private void PositionAtBottomCenter()
    {
        if (Screens?.Primary is not { } screen) return;

        var workArea = screen.WorkingArea;
        var windowWidth = (int)(Bounds.Width * screen.Scaling);
        var windowHeight = (int)(Bounds.Height * screen.Scaling);

        var x = workArea.X + (workArea.Width - windowWidth) / 2;
        var y = workArea.Bottom - windowHeight - (int)(50 * screen.Scaling);

        Position = new PixelPoint(x, y);
    }
}
