using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class WallpaperPickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private bool _isWallpaperSwitchEnabled;

    [ObservableProperty]
    private int _selectIndexWallpaperMode;

    public WallpaperPickerViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "TimePage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        IsWallpaperSwitchEnabled = _builder.Config.WallpaperSwitch.Enabled;
        if (App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light)
        {
            SelectIndexWallpaperMode = 0;
        }
        else
        {
            SelectIndexWallpaperMode = 1;
        }
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StateUpdateHandler.StopConfigWatcher();

            _builder.Load();
            LoadSettings();

            StateUpdateHandler.StartConfigWatcher();
        });
    }

    partial void OnIsWallpaperSwitchEnabledChanged(bool value)
    {
        _builder.Config.WallpaperSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickPage");
        }
    }
}
