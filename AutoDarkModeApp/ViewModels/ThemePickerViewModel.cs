﻿using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;

using static AutoDarkModeLib.IThemeManager2.Flags;

namespace AutoDarkModeApp.ViewModels;

public partial class ThemePickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    private bool _isInitializing;

    [ObservableProperty]
    public partial bool ThemeSwitchEnabled { get; set; }

    [ObservableProperty]
    public partial bool ThemeKeepActiveEnabled { get; set; }

    [ObservableProperty]
    public partial object? SelectedLightTheme { get; set; }

    [ObservableProperty]
    public partial object? SelectedDarkTheme { get; set; }

    [ObservableProperty]
    public partial bool IgnoreBackgroundEnabled { get; set; }

    [ObservableProperty]
    public partial bool IgnoreCursorEnabled { get; set; }

    [ObservableProperty]
    public partial bool IgnoreSoundEnabled { get; set; }

    [ObservableProperty]
    public partial bool IgnoreColorEnabled { get; set; }

    [ObservableProperty]
    public partial bool IgnoreDesktopIconsEnabled { get; set; }

    public ThemePickerViewModel(IErrorService errorService)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _errorService = errorService;

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ThemePickerPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        ThemeSwitchEnabled = _builder.Config.WindowsThemeMode.Enabled;
        ThemeKeepActiveEnabled = _builder.Config.WindowsThemeMode.MonitorActiveTheme;

        var applyFlags = _builder.Config.WindowsThemeMode.ApplyFlags;
        var flagsSet = new HashSet<ThemeApplyFlags>(applyFlags);
        IgnoreBackgroundEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreBackground);
        IgnoreCursorEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreCursor);
        IgnoreDesktopIconsEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreDesktopIcons);
        IgnoreSoundEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreSound);
        IgnoreColorEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreColor);

        _isInitializing = false;
    }

    private void WriteSettings()
    {
        if (_isInitializing)
            return;

        List<ThemeApplyFlags> flags = [];
        if (IgnoreBackgroundEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreBackground);
        if (IgnoreCursorEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreCursor);
        if (IgnoreSoundEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreSound);
        if (IgnoreDesktopIconsEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreDesktopIcons);
        if (IgnoreColorEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreColor);
        _builder.Config.WindowsThemeMode.ApplyFlags = flags;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CheckBoxMonitorActiveTheme");
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

    partial void OnThemeSwitchEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.WindowsThemeMode.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ThemePickerPage");
        }
        RequestThemeSwitch();
    }

    partial void OnThemeKeepActiveEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.WindowsThemeMode.MonitorActiveTheme = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CheckBoxMonitorActiveTheme");
        }
    }

    partial void OnSelectedLightThemeChanged(object? value)
    {
        if (_isInitializing)
            return;

        List<ThemeFile> themeCollection = ThemeCollectionHandler.GetUserThemes();
        IEnumerable<string> themeNames = themeCollection.Select(t => t.ToString());
        try
        {
            if (value != null)
            {
                ThemeFile? selected = themeCollection.FirstOrDefault(t => t.ToString().Contains(value.ToString()!));
                if (selected != null)
                    _builder.Config.WindowsThemeMode.LightThemePath = selected.Path;
            }
        }
        catch
        {
            SelectedLightTheme = null;
        }

        if (ThemeSwitchEnabled)
        {
            _builder.Config.WallpaperSwitch.Enabled = false;
            _builder.Config.WindowsThemeMode.Enabled = true;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SaveThemeSettings");
        }
        RequestThemeSwitch();
    }

    partial void OnSelectedDarkThemeChanged(object? value)
    {
        if (_isInitializing)
            return;

        List<ThemeFile> themeCollection = ThemeCollectionHandler.GetUserThemes();
        IEnumerable<string> themeNames = themeCollection.Select(t => t.ToString());
        try
        {
            if (value != null)
            {
                ThemeFile? selected = themeCollection.FirstOrDefault(t => t.ToString().Contains(value.ToString()!));
                if (selected != null)
                    _builder.Config.WindowsThemeMode.DarkThemePath = selected.Path;
            }
        }
        catch
        {
            SelectedDarkTheme = null;
        }

        if (ThemeSwitchEnabled)
        {
            _builder.Config.WallpaperSwitch.Enabled = false;
            _builder.Config.WindowsThemeMode.Enabled = true;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "SaveThemeSettings");
        }
        RequestThemeSwitch();
    }

    partial void OnIgnoreBackgroundEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIgnoreCursorEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIgnoreSoundEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIgnoreDesktopIconsEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIgnoreColorEnabledChanged(bool value)
    {
        WriteSettings();
    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "ThemePickerViewModel");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ThemePickerViewModel");
        }
    }

}
