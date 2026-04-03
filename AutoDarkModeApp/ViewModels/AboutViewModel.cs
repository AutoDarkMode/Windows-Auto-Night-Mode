using AutoDarkModeApp.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class AboutViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial string? CommitHashText { get; set; }

    [ObservableProperty]
    public partial string? SvcVersionText { get; set; }

    [ObservableProperty]
    public partial string? UpdaterVersionText { get; set; }

    [ObservableProperty]
    public partial string? ShellVersionText { get; set; }

    [ObservableProperty]
    public partial string? DotNetVersionText { get; set; }

    [ObservableProperty]
    public partial string? WindowsVersionText { get; set; }

    [ObservableProperty]
    public partial string? ArchText { get; set; }

    public AboutViewModel()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        var versionInfo = new VersionInfo();
        CommitHashText = versionInfo.Commit;
        SvcVersionText = versionInfo.Svc;
        UpdaterVersionText = versionInfo.Updater;
        ShellVersionText = versionInfo.Shell;
        DotNetVersionText = versionInfo.NetCore;
        WindowsVersionText = versionInfo.WindowsVersion;
        ArchText = versionInfo.Arch;
    }

}
