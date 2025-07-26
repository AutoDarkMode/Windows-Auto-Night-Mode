using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class ColorizationViewModel : ObservableRecipient
{
    private const string Location = "ColorizationViewModel";
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    public enum ThemeColorMode
    {
        Automatic,
        Manual,
    }

    [ObservableProperty]
    public partial bool IsColorizationEnabled { get; set; }

    [ObservableProperty]
    public partial ThemeColorMode LightThemeMode { get; set; }

    [ObservableProperty]
    public partial ThemeColorMode DarkThemeMode { get; set; }

    [ObservableProperty]
    public partial bool IsLightThemeSettingsCardEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsDarkThemeSettingsCardEnabled { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? LightModeColorPreviewBorderBackground { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? DarkModeColorPreviewBorderBackground { get; set; }

    public ColorizationViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        IsColorizationEnabled = _builder.Config.ColorizationSwitch.Enabled;
        if (_builder.Config.ColorizationSwitch.Component.LightAutoColorization)
        {
            LightThemeMode = ThemeColorMode.Automatic;
            IsLightThemeSettingsCardEnabled = false;
        }
        else
        {
            LightThemeMode = ThemeColorMode.Manual;
            IsLightThemeSettingsCardEnabled = true;
        }

        if (_builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
        {
            DarkThemeMode = ThemeColorMode.Automatic;
            IsDarkThemeSettingsCardEnabled = false;
        }
        else
        {
            DarkThemeMode = ThemeColorMode.Manual;
            IsDarkThemeSettingsCardEnabled = true;
        }

        LightModeColorPreviewBorderBackground = new SolidColorBrush(_builder.Config.ColorizationSwitch.Component.LightHex.ToColor());
        DarkModeColorPreviewBorderBackground = new SolidColorBrush(_builder.Config.ColorizationSwitch.Component.DarkHex.ToColor());
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

    partial void OnIsColorizationEnabledChanged(bool value)
    {
        _builder.Config.ColorizationSwitch.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnLightThemeModeChanged(ThemeColorMode value)
    {
        switch (value)
        {
            case ThemeColorMode.Automatic:
                _builder.Config.ColorizationSwitch.Component.LightAutoColorization = true;
                IsLightThemeSettingsCardEnabled = false;
                Debug.WriteLine(IsLightThemeSettingsCardEnabled);
                break;
            default:
                _builder.Config.ColorizationSwitch.Component.LightAutoColorization = false;
                IsLightThemeSettingsCardEnabled = true;
                break;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnDarkThemeModeChanged(ThemeColorMode value)
    {
        switch (value)
        {
            case ThemeColorMode.Automatic:
                _builder.Config.ColorizationSwitch.Component.DarkAutoColorization = true;
                IsDarkThemeSettingsCardEnabled = false;
                break;
            default:
                _builder.Config.ColorizationSwitch.Component.DarkAutoColorization = false;
                IsDarkThemeSettingsCardEnabled = true;
                break;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
