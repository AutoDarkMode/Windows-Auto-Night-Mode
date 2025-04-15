using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Handlers;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using static AutoDarkModeLib.IThemeManager2.Flags;

namespace AutoDarkModeApp.ViewModels;

public partial class ThemePickerViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;

    [ObservableProperty]
    public partial bool IsThemeSwitchEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsThemeKeepActiveEnabled { get; set; }

    [ObservableProperty]
    public partial object? SelectLightTheme { get; set; }

    [ObservableProperty]
    public partial object? SelectDarkTheme { get; set; }

    [ObservableProperty]
    public partial bool IsIgnoreBackgroundEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsIgnoreCursorEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsIgnoreSoundEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsIgnoreDesktopIconsEnabled { get; set; }

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
        IsThemeSwitchEnabled = _builder.Config.WindowsThemeMode.Enabled;
        IsThemeKeepActiveEnabled = _builder.Config.WindowsThemeMode.MonitorActiveTheme;

        var applyFlags = _builder.Config.WindowsThemeMode.ApplyFlags;
        var flagsSet = new HashSet<ThemeApplyFlags>(applyFlags);
        IsIgnoreBackgroundEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreBackground);
        IsIgnoreCursorEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreCursor);
        IsIgnoreDesktopIconsEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreDesktopIcons);
        IsIgnoreSoundEnabled = flagsSet.Contains(ThemeApplyFlags.IgnoreSound);
    }

    private void WriteSettings()
    {
        List<ThemeApplyFlags> flags = new();
        if (IsIgnoreBackgroundEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreBackground);
        if (IsIgnoreCursorEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreCursor);
        if (IsIgnoreSoundEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreSound);
        if (IsIgnoreDesktopIconsEnabled == true)
            flags.Add(ThemeApplyFlags.IgnoreDesktopIcons);
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

    partial void OnIsThemeSwitchEnabledChanged(bool value)
    {
        _builder.Config.WindowsThemeMode.Enabled = value;
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ThemePickerPage");
        }
    }

    partial void OnIsThemeKeepActiveEnabledChanged(bool value)
    {
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

    partial void OnSelectLightThemeChanged(object? value)
    {
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
            SelectLightTheme = null;
        }

        if (IsThemeSwitchEnabled)
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
    }

    partial void OnSelectDarkThemeChanged(object? value)
    {
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
            SelectDarkTheme = null;
        }

        if (IsThemeSwitchEnabled)
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
    }

    partial void OnIsIgnoreBackgroundEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIsIgnoreCursorEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIsIgnoreSoundEnabledChanged(bool value)
    {
        WriteSettings();
    }

    partial void OnIsIgnoreDesktopIconsEnabledChanged(bool value)
    {
        WriteSettings();
    }

    internal void OnViewModelNavigatedFrom(NavigationEventArgs e)
    {
        StateUpdateHandler.OnConfigUpdate -= HandleConfigUpdate;
        StateUpdateHandler.StopConfigWatcher();
    }
}
