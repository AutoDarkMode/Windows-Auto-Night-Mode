using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class HotkeysViewModel : ObservableRecipient
{
    private const string Location = "HotkeysViewModel";
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly IErrorService _errorService;
    private bool _isInitializing;

    [ObservableProperty]
    public partial bool HotkeysEnabled { get; set; }

    [ObservableProperty]
    public partial bool HotkeyToggleAutomaticThemeNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial bool HotkeyTogglePostponeNotificationEnabled { get; set; }

    public HotkeysViewModel(IErrorService errorService)
    {
        _errorService = errorService;

        try
        {
            _builder.Load();
            _builder.LoadLocationData();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }

        LoadSettings();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        HotkeysEnabled = _builder.Config.Hotkeys.Enabled;
        HotkeyToggleAutomaticThemeNotificationEnabled = _builder.Config.Notifications.OnAutoThemeSwitching;
        HotkeyTogglePostponeNotificationEnabled = _builder.Config.Notifications.OnSkipNextSwitch;

        _isInitializing = false;
    }

    partial void OnHotkeysEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Hotkeys.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnHotkeyToggleAutomaticThemeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Notifications.OnAutoThemeSwitching = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }

    partial void OnHotkeyTogglePostponeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.Notifications.OnSkipNextSwitch = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, Location);
        }
    }
}