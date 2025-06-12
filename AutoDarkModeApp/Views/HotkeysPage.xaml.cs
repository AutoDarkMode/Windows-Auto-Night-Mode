using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            Title = "ActivationShortcut".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "Save".GetLocalized(),
            SecondaryButtonText = "Reset".GetLocalized(),
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

        var collection = (ObservableCollection<HotkeysDataObject>)HotkeysItemView.ItemsSource;
        var tag = hotkeyData.Tag;

        var propertyMap = new Dictionary<string, (Action<string> Setter, string Tag)>
        {
            ["ForceLight"] = (v => _builder.Config.Hotkeys.ForceLight = v, "ForceLight"),
            ["ForceDark"] = (v => _builder.Config.Hotkeys.ForceDark = v, "ForceDark"),
            ["StopForcing"] = (v => _builder.Config.Hotkeys.NoForce = v, "StopForcing"),
            ["ToggleTheme"] = (v => _builder.Config.Hotkeys.ToggleTheme = v, "ToggleTheme"),
            ["AutomaticThemeSwitch"] = (v => _builder.Config.Hotkeys.ToggleAutoThemeSwitch = v, "AutomaticThemeSwitch"),
            ["PauseAutoThemeSwitching"] = (v => _builder.Config.Hotkeys.TogglePostpone = v, "PauseAutoThemeSwitching"),
        };

        if (propertyMap.TryGetValue(tag!, out var config))
        {
            config.Setter(dialogContent.CapturedHotkeys!);
            if (collection.FirstOrDefault(h => h.Tag == config.Tag) is { } itemToUpdate)
            {
                itemToUpdate.Keys = dialogContent.CapturedHotkeys;
            }
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, this.XamlRoot, "HotkeysPage");
        }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var collection = (ObservableCollection<HotkeysDataObject>)HotkeysItemView.ItemsSource;

        _builder.Config.Hotkeys.ForceLight = collection.FirstOrDefault(h => h.Tag == "ForceLight")?.Keys;
        _builder.Config.Hotkeys.ForceDark = collection.FirstOrDefault(h => h.Tag == "ForceDark")?.Keys;
        _builder.Config.Hotkeys.NoForce = collection.FirstOrDefault(h => h.Tag == "StopForcing")?.Keys;
        _builder.Config.Hotkeys.ToggleTheme = collection.FirstOrDefault(h => h.Tag == "ToggleTheme")?.Keys;
        _builder.Config.Hotkeys.ToggleAutoThemeSwitch = collection.FirstOrDefault(h => h.Tag == "AutomaticThemeSwitch")?.Keys;
        _builder.Config.Hotkeys.TogglePostpone = collection.FirstOrDefault(h => h.Tag == "PauseAutoThemeSwitching")?.Keys;

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, this.XamlRoot, "HotkeysPage");
        }
    }
}

public partial class HotkeysDataObject : INotifyPropertyChanged
{
    private string? _name { get; set; }
    private string? _keys { get; set; }
    private string? _tag { get; set; }

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string? Keys
    {
        get => _keys;
        set
        {
            _keys = value;
            OnPropertyChanged();
        }
    }

    public string? Tag
    {
        get => _tag;
        set
        {
            _tag = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
