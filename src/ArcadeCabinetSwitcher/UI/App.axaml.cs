using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ArcadeCabinetSwitcher.UI;

public partial class App : Application
{
    internal static AvaloniaOverlayService? OverlayService { get; set; }

    private TrayIconManager? _trayIconManager;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => _trayIconManager?.Dispose();

            if (OverlayService is not null)
            {
                var overlayWindow = new OverlayWindow();
                _trayIconManager = new TrayIconManager(this);
                OverlayService.Initialize(overlayWindow, _trayIconManager);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
