using System.Collections.ObjectModel;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class HotkeysPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public HotkeysViewModel ViewModel { get; }

    public HotkeysPage()
    {
        ViewModel = App.GetService<HotkeysViewModel>();
        InitializeComponent();

        try
        {
            _builder.Load();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, this.XamlRoot, "HotkeysPage");
        }

        LoadSettings();
    }

    private void LoadSettings()
    {
        HotkeysItemView.ItemsSource = new ObservableCollection<HotkeysDataObject>
        {
            new()
            {
                Name = "ForceLight".GetLocalized(),
                Keys = _builder.Config.Hotkeys.ForceLight,
                Tag = "ForceLight",
            },
            new()
            {
                Name = "ForceDark".GetLocalized(),
                Keys = _builder.Config.Hotkeys.ForceDark,
                Tag = "ForceDark",
            },
            new()
            {
                Name = "StopForcing".GetLocalized(),
                Keys = _builder.Config.Hotkeys.NoForce,
                Tag = "StopForcing",
            },
            new()
            {
                Name = "ToggleTheme".GetLocalized(),
                Keys = _builder.Config.Hotkeys.ToggleTheme,
                Tag = "ToggleTheme",
            },
            new()
            {
                Name = "AutomaticThemeSwitch".GetLocalized(),
                Keys = _builder.Config.Hotkeys.ToggleAutoThemeSwitch,
                Tag = "AutomaticThemeSwitch",
            },
            new()
            {
                Name = "PauseAutoThemeSwitching".GetLocalized(),
                Keys = _builder.Config.Hotkeys.TogglePostpone,
                Tag = "PauseAutoThemeSwitching",
            },
        };
    }

    private async void EditHotkeysButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not HotkeysDataObject hotkeyData)
            return;

        var keyString = hotkeyData.Tag switch
        {
            "ForceLight" => _builder.Config.Hotkeys.ForceLight,
            "ForceDark" => _builder.Config.Hotkeys.ForceDark,
            "StopForcing" => _builder.Config.Hotkeys.NoForce,
            "ToggleTheme" or "AutomaticThemeSwitch" => _builder.Config.Hotkeys.ToggleAutoThemeSwitch,
            "PauseAutoThemeSwitching" => _builder.Config.Hotkeys.TogglePostpone,
            _ => null,
        };

        var dialogContent = new ShortcutDialogContentControl();

        if (keyString != null)
        {
            dialogContent.Keys = keyString.Split(" + ").Select(key => new SingleHotkeyDataObject { Key = key }).ToList();
        }

        var shortcutDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "Activate shortcut key",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Reset",
            Content = dialogContent,
        };

        var result = await shortcutDialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            dialogContent.Keys?.Clear();
            dialogContent.CapturedHotkeys = null;
        }
        else if (result != ContentDialogResult.Primary)
        {
            return;
        }

        switch (hotkeyData.Tag)
        {
            case "ForceLight":
                _builder.Config.Hotkeys.ForceLight = dialogContent.CapturedHotkeys;
                break;
            case "ForceDark":
                _builder.Config.Hotkeys.ForceDark = dialogContent.CapturedHotkeys;
                break;
            case "StopForcing":
                _builder.Config.Hotkeys.NoForce = dialogContent.CapturedHotkeys;
                break;
            case "ToggleTheme":
                _builder.Config.Hotkeys.ToggleTheme = dialogContent.CapturedHotkeys;
                break;
            case "AutomaticThemeSwitch":
                _builder.Config.Hotkeys.ToggleAutoThemeSwitch = dialogContent.CapturedHotkeys;
                break;
            case "PauseAutoThemeSwitching":
                _builder.Config.Hotkeys.TogglePostpone = dialogContent.CapturedHotkeys;
                break;
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, this.XamlRoot, "HotkeysPage");
        }
        LoadSettings();
    }
}

public class HotkeysDataObject
{
    public string? Name { get; set; }
    public string? Keys { get; set; }
    public string? Tag { get; set; }
}
