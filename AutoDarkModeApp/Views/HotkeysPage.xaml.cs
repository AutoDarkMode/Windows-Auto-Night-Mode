using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
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
        var hotkeyConfigs = new[]
        {
            new { NameKey = "ForceLight", ConfigKey = nameof(Hotkeys.ForceLight) },
            new { NameKey = "ForceDark", ConfigKey = nameof(Hotkeys.ForceDark) },
            new { NameKey = "StopForcing", ConfigKey = nameof(Hotkeys.NoForce) },
            new { NameKey = "ToggleTheme", ConfigKey = nameof(Hotkeys.ToggleTheme) },
            new { NameKey = "AutomaticThemeSwitch", ConfigKey = nameof(Hotkeys.ToggleAutoThemeSwitch) },
            new { NameKey = "PauseAutoThemeSwitching", ConfigKey = nameof(Hotkeys.TogglePostpone) },
        };

        var hotkeysCollection = new ObservableCollection<HotkeysDataObject>(
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

        HotkeysItemView.ItemsSource = hotkeysCollection;
    }

    private async void EditHotkeysButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not HotkeysDataObject hotkeyData)
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
            dialogContent.HotkeyCombination = keyString.Split(" + ").Select(key => new SingleHotkeyDataObject { Key = key }).ToList();
        }

        var shortcutDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "ActivationShortcut".GetLocalized() + " - " + hotkeyData.Tag,
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            SecondaryButtonText = "Reset".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };

        var result = await shortcutDialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            dialogContent.HotkeyCombination?.Clear();
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
    private string? _displayName { get; set; }
    private string? _keys { get; set; }
    private string? _tag { get; set; }

    public string? DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
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
