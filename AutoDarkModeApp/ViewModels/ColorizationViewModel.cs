using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.ViewModels;

public partial class ColorizationViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;

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
    public partial bool ThemeSettingsExpanderVisibility { get; set; }

    [ObservableProperty]
    public partial bool LightThemeColorizationSettingsCardEnabled { get; set; }

    [ObservableProperty]
    public partial bool DarkThemeColorizationSettingsCardEnabled { get; set; }

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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
        }

        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        IsColorizationEnabled = _builder.Config.ColorizationSwitch.Enabled;
        ThemeSettingsExpanderVisibility = _builder.Config.ColorizationSwitch.Enabled;

        const string DEFAULT_HEX_COLOR = "#C40078D4";

        var messageRaw = MessageHandler.Client.SendMessageAndGetReply(Command.GetCurrentColorization);
        ApiResponse response = ApiResponse.FromString(messageRaw);
        var config = _builder.Config.ColorizationSwitch.Component;
        var needsSave = false;

        if (response.StatusCode == StatusCode.Ok)
        {
            // Initialize undefined hex colors
            if (string.IsNullOrEmpty(config.LightHex))
            {
                config.LightHex = response.Message;
                needsSave = true;
            }
            if (string.IsNullOrEmpty(config.DarkHex))
            {
                config.DarkHex = response.Message;
                needsSave = true;
            }

            // Update hex for auto colorization if enabled
            if (config.LightAutoColorization || config.DarkAutoColorization)
            {
                if (Enum.TryParse<Theme>(response.Details, out var requestedTheme))
                {
                    if (config.LightAutoColorization && requestedTheme == Theme.Light)
                    {
                        config.LightHex = response.Message;
                        needsSave = true;
                    }
                    if (config.DarkAutoColorization && requestedTheme == Theme.Dark)
                    {
                        config.DarkHex = response.Message;
                        needsSave = true;
                    }
                }
                else
                {
                    _errorService.ShowErrorMessage(new ArgumentException($"Invalid theme value: {response.Details}"), App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
                }
            }
        }
        else
        {
            _errorService.ShowErrorMessageFromApi(response, App.MainWindow.Content.XamlRoot);

            // Set default colors if not defined
            if (string.IsNullOrEmpty(config.LightHex))
            {
                config.LightHex = DEFAULT_HEX_COLOR;
                needsSave = true;
            }
            if (string.IsNullOrEmpty(config.DarkHex))
            {
                config.DarkHex = DEFAULT_HEX_COLOR;
                needsSave = true;
            }
        }
        if (needsSave)
        {
            SafeSaveBuilder();
        }

        if (_builder.Config.ColorizationSwitch.Component.LightAutoColorization)
        {
            LightThemeMode = ThemeColorMode.Automatic;
            LightThemeColorizationSettingsCardEnabled = false;
        }
        else
        {
            LightThemeMode = ThemeColorMode.Manual;
            LightThemeColorizationSettingsCardEnabled = true;
        }
        if (_builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
        {
            DarkThemeMode = ThemeColorMode.Automatic;
            DarkThemeColorizationSettingsCardEnabled = false;
        }
        else
        {
            DarkThemeMode = ThemeColorMode.Manual;
            DarkThemeColorizationSettingsCardEnabled = true;
        }

        var lightColorizationColor = _builder.Config.ColorizationSwitch.Component.LightHex.ToColor();
        lightColorizationColor.A = 255;
        var darkColorizationColor = _builder.Config.ColorizationSwitch.Component.DarkHex.ToColor();
        darkColorizationColor.A = 255;

        LightModeColorPreviewBorderBackground = new SolidColorBrush(lightColorizationColor);
        DarkModeColorPreviewBorderBackground = new SolidColorBrush(darkColorizationColor);

        _isInitializing = false;
    }

    private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
    {
        if (_isInitializing)
            return;

        StateUpdateHandler.StopConfigWatcher();
        _dispatcherQueue.TryEnqueue(() =>
        {
            _builder.Load();
            LoadSettings();
        });
        StateUpdateHandler.StartConfigWatcher();
    }

    private void SafeSaveBuilder()
    {
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }
    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "ColorizationViewModel");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }
    }

    private async Task WaitForColorizationChange(string initial, int timeout)
    {
        if (!_builder.Config.ColorizationSwitch.Enabled)
            return;

        for (int tries = 0; tries < timeout; tries++)
        {
            var messageRaw = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetCurrentColorization);
            var response = ApiResponse.FromString(messageRaw);

            if (response.StatusCode == StatusCode.Ok && !response.Message.Equals(initial, StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.WriteLine($"Colorization change detected, updating UI\n{response.Message}");
                break;
            }

            await Task.Delay(1000);
        }
    }

    partial void OnIsColorizationEnabledChanged(bool value)
    {
        if (_isInitializing)
            return;

        _builder.Config.ColorizationSwitch.Enabled = value;

        SafeSaveBuilder();
    }

    partial void OnLightThemeModeChanged(ThemeColorMode value)
    {
        if (_isInitializing)
            return;

        _isInitializing = true;

        if (value == ThemeColorMode.Automatic)
        {
            _builder.Config.ColorizationSwitch.Component.LightAutoColorization = true;
            LightThemeColorizationSettingsCardEnabled = false;
        }
        else
        {
            _builder.Config.ColorizationSwitch.Component.LightAutoColorization = false;
            LightThemeColorizationSettingsCardEnabled = true;
        }

        SafeSaveBuilder();
        try
        {
            RequestThemeSwitch();
            if (_builder.Config.ColorizationSwitch.Component.LightAutoColorization)
            {
                Task.Run(async () => await WaitForColorizationChange(_builder.Config.ColorizationSwitch.Component.LightHex.ToLower(), 5));
                _dispatcherQueue.TryEnqueue(() => LoadSettings());
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }

        _isInitializing = false;
    }

    partial void OnDarkThemeModeChanged(ThemeColorMode value)
    {
        if (_isInitializing)
            return;

        _isInitializing = true;

        if (value == ThemeColorMode.Automatic)
        {
            _builder.Config.ColorizationSwitch.Component.DarkAutoColorization = true;
            DarkThemeColorizationSettingsCardEnabled = false;
        }
        else
        {
            _builder.Config.ColorizationSwitch.Component.DarkAutoColorization = false;
            DarkThemeColorizationSettingsCardEnabled = true;
        }

        SafeSaveBuilder();
        try
        {
            RequestThemeSwitch();
            if (_builder.Config.ColorizationSwitch.Component.DarkAutoColorization)
            {
                Task.Run(async () => await WaitForColorizationChange(_builder.Config.ColorizationSwitch.Component.DarkHex.ToLower(), 5));
                _dispatcherQueue.TryEnqueue(() => LoadSettings());
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }

        _isInitializing = false;
    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "ColorizationViewModel");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }
    }
}
