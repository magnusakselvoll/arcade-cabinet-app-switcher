namespace ArcadeCabinetSwitcher.UI;

public interface IOverlayService
{
    void ShowProfileNotification(string profileName);
    void UpdateActiveProfile(string profileName);
    void SetAvailableProfiles(IReadOnlyList<string> profileNames);
    event EventHandler<string>? ProfileSwitchRequested;
    event EventHandler? ExitRequested;
}
