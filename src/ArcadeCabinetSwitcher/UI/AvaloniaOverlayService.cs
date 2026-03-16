using Avalonia.Threading;

namespace ArcadeCabinetSwitcher.UI;

public class AvaloniaOverlayService : IOverlayService
{
    private readonly object _lock = new();
    private readonly Queue<Action> _pending = new();
    private bool _uiReady;

    private OverlayWindow? _overlayWindow;
    private TrayIconManager? _trayIconManager;

    public event EventHandler<string>? ProfileSwitchRequested;
    public event EventHandler? ExitRequested;

    internal void Initialize(OverlayWindow overlayWindow, TrayIconManager trayIconManager)
    {
        // Called on UI thread from App.OnFrameworkInitializationCompleted
        _overlayWindow = overlayWindow;
        _trayIconManager = trayIconManager;

        trayIconManager.ProfileSwitchRequested += (_, name) =>
            ProfileSwitchRequested?.Invoke(this, name);
        trayIconManager.ExitRequested += (_, _) =>
            ExitRequested?.Invoke(this, EventArgs.Empty);

        List<Action> drained;
        lock (_lock)
        {
            _uiReady = true;
            drained = [.. _pending];
            _pending.Clear();
        }

        // Execute buffered actions directly — we are on the UI thread
        foreach (var action in drained)
            action();
    }

    private void RunOnUI(Action action)
    {
        lock (_lock)
        {
            if (!_uiReady)
            {
                _pending.Enqueue(action);
                return;
            }
        }

        Dispatcher.UIThread.Post(action);
    }

    public void ShowProfileNotification(string profileName) =>
        RunOnUI(() => _overlayWindow?.ShowNotification(profileName));

    public void UpdateActiveProfile(string profileName) =>
        RunOnUI(() => _trayIconManager?.UpdateActiveProfile(profileName));

    public void SetAvailableProfiles(IReadOnlyList<string> profileNames) =>
        RunOnUI(() => _trayIconManager?.SetAvailableProfiles(profileNames));
}
