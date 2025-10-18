using System.Collections.ObjectModel;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Models;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoDarkModeApp.ViewModels;

public partial class HotkeysViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;
    private bool _skipConfigUpdate;

    [ObservableProperty]
    public partial bool HotkeysEnabled { get; set; }

    [ObservableProperty]
    public partial bool HotkeyToggleAutomaticThemeNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial bool HotkeyTogglePostponeNotificationEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<HotkeysDataObject>? HotkeysCollection { get; set; }

    public AdmConfig GetAdmConfig()
    {
        return _builder.Config;
    }

    public void SafeSaveBuilder()
    {
        try
        {
            _skipConfigUpdate = true;
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "HotkeysViewModel");
        }
    }

    public void ForceSaveHotkeysSettings()
    {
        if (HotkeysCollection is not null)
        {
            _builder.Config.Hotkeys.ForceLight = HotkeysCollection.FirstOrDefault(h => h.Tag == "ForceLight")?.Keys;
            _builder.Config.Hotkeys.ForceDark = HotkeysCollection.FirstOrDefault(h => h.Tag == "ForceDark")?.Keys;
            _builder.Config.Hotkeys.NoForce = HotkeysCollection.FirstOrDefault(h => h.Tag == "StopForcing")?.Keys;
            _builder.Config.Hotkeys.ToggleTheme = HotkeysCollection.FirstOrDefault(h => h.Tag == "ToggleTheme")?.Keys;
            _builder.Config.Hotkeys.ToggleAutoThemeSwitch = HotkeysCollection.FirstOrDefault(h => h.Tag == "AutomaticThemeSwitch")?.Keys;
            _builder.Config.Hotkeys.TogglePostpone = HotkeysCollection.FirstOrDefault(h => h.Tag == "PauseAutoThemeSwitching")?.Keys;
        }
    }

    public string? GetHotkeyValue(string tag)
    {
        return tag switch
        {
            "ForceLight" => _builder.Config.Hotkeys.ForceLight,
            "ForceDark" => _builder.Config.Hotkeys.ForceDark,
            "StopForcing" => _builder.Config.Hotkeys.NoForce,
            "ToggleTheme" or "AutomaticThemeSwitch" => _builder.Config.Hotkeys.ToggleAutoThemeSwitch,
            "PauseAutoThemeSwitching" => _builder.Config.Hotkeys.TogglePostpone,
            _ => null,
        };
    }

    public void UpdateHotkeyValue(string tag, string? value)
    {
        if (HotkeysCollection is null)
        {
            return;
        }

        switch (tag)
        {
            case "ForceLight":
                _builder.Config.Hotkeys.ForceLight = value;
                break;
            case "ForceDark":
                _builder.Config.Hotkeys.ForceDark = value;
                break;
            case "StopForcing":
                _builder.Config.Hotkeys.NoForce = value;
                break;
            case "ToggleTheme":
                _builder.Config.Hotkeys.ToggleTheme = value;
                break;
            case "AutomaticThemeSwitch":
                _builder.Config.Hotkeys.ToggleAutoThemeSwitch = value;
                break;
            case "PauseAutoThemeSwitching":
                _builder.Config.Hotkeys.TogglePostpone = value;
                break;
        }

        var item = HotkeysCollection.FirstOrDefault(h => h.Tag == tag);
        item!.Keys = value;

        SafeSaveBuilder();
    }

    public HotkeysViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "HotkeysViewModel");
        }

        LoadSettings();
        LoadHotkeysList();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        HotkeysEnabled = _builder.Config.Hotkeys.Enabled;
        HotkeyToggleAutomaticThemeNotificationEnabled = _builder.Config.Notifications.OnAutoThemeSwitching;
        HotkeyTogglePostponeNotificationEnabled = _builder.Config.Notifications.OnSkipNextSwitch;

        _isInitializing = false;
    }

    private void LoadHotkeysList()
    {
        var hotkeyConfigs = new[]
        {
            new { NameKey = "ForceLight", ConfigKey = nameof(Hotkeys.ForceLight) },
            new { NameKey = "ForceDark", ConfigKey = nameof(Hotkeys.ForceDark) },
            new { NameKey = "StopForcing", ConfigKey = nameof(Hotkeys.NoForce) },
            new { NameKey = "ToggleTheme", ConfigKey = nameof(Hotkeys.ToggleTheme) },
            new { NameKey = "AutomaticThemeSwitch", ConfigKey = nameof(Hotkeys.ToggleAutoThemeSwitch) },
            new { NameKey = "PauseAutoThemeSwitching", ConfigKey = nameof(Hotkeys.TogglePostpone) },
        };

        HotkeysCollection = new ObservableCollection<HotkeysDataObject>(
            hotkeyConfigs.Select(cfg =>
            {
                var propertyInfo = typeof(Hotkeys).GetProperty(cfg.ConfigKey) ?? throw new InvalidOperationException($"Property '{cfg.ConfigKey}' not found on type 'Hotkeys'.");
                return new HotkeysDataObject
                {
                    DisplayName = cfg.NameKey.GetLocalized(),
                    Keys = (string?)propertyInfo.GetValue(_builder.Config.Hotkeys),
                    Tag = cfg.NameKey,
                };
            })
        );
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        StateUpdateHandler.StopConfigWatcher();
        if (_skipConfigUpdate)
        {
            _skipConfigUpdate = false;
            StateUpdateHandler.StartConfigWatcher();
        }
        else
        {
            _builder.Load();
            _dispatcherQueue.TryEnqueue(() => LoadSettings());
            StateUpdateHandler.StartConfigWatcher();
        }
    }

    partial void OnHotkeysEnabledChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _builder.Config.Hotkeys.Enabled = value;

        SafeSaveBuilder();
    }

    partial void OnHotkeyToggleAutomaticThemeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _builder.Config.Notifications.OnAutoThemeSwitching = value;

        SafeSaveBuilder();
    }

    partial void OnHotkeyTogglePostponeNotificationEnabledChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _builder.Config.Notifications.OnSkipNextSwitch = value;

        SafeSaveBuilder();
    }
}
