using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.Configs;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.ViewModels;

public partial class ColorizationViewModel : ObservableRecipient
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly IErrorService _errorService;
    private bool _isInitializing;
    private bool _skipConfigUpdate;

    public enum ThemeColorMode
    {
        Automatic,
        Manual,
    }

    [ObservableProperty]
    public partial ElementTheme DesktopPreviewThemeMode { get; set; }

    [ObservableProperty]
    public partial bool DesktopPreviewAccentColorBorderVisible { get; set; }

    [ObservableProperty]
    public partial bool IsColorizationEnabled { get; set; }

    [ObservableProperty]
    public partial ApplicationTheme SelectColorThemeMode { get; set; }

    [ObservableProperty]
    public partial ThemeColorMode AccentColorMode { get; set; }

    [ObservableProperty]
    public partial bool ManualColorSelectionSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial bool AutomaticColorPromptSettingsCardVisibility { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? AccentColorPreviewBorderBackground { get; set; }

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
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }
    }

    public async void RequestThemeSwitch()
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

        InitializeAccentColor();
        SelectColorThemeMode = Application.Current.RequestedTheme;
        LoadSettings();

        StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
        StateUpdateHandler.StartConfigWatcher();
    }

    private void LoadSettings()
    {
        _isInitializing = true;

        DesktopPreviewThemeMode = SelectColorThemeMode == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;

        IsColorizationEnabled = _builder.Config.ColorizationSwitch.Enabled;

        var config = _builder.Config.ColorizationSwitch.Component;
        var isLightTheme = SelectColorThemeMode == ApplicationTheme.Light;
        var isAutoColorization = isLightTheme ? config.LightAutoColorization : config.DarkAutoColorization;

        if (isAutoColorization)
        {
            AccentColorMode = ThemeColorMode.Automatic;
        }
        else
        {
            AccentColorMode = ThemeColorMode.Manual;
            var colorizationColor = isLightTheme ? config.LightHex.ToColor() : config.DarkHex.ToColor();
            colorizationColor.A = 255;
            DesktopPreviewAccentColorBorderVisible = true;
            AccentColorPreviewBorderBackground = new SolidColorBrush(colorizationColor);
        }

        AutomaticColorPromptSettingsCardVisibility = AccentColorMode == ThemeColorMode.Automatic;
        ManualColorSelectionSettingsCardVisibility = AccentColorMode == ThemeColorMode.Manual;

        _isInitializing = false;
    }

    private void InitializeAccentColor()
    {
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

    private async Task WaitForColorizationChange(string initial, int timeout)
    {
        if (!_builder.Config.ColorizationSwitch.Enabled)
        {
            return;
        }

        for (int tries = 0; tries < timeout; tries++)
        {
            var messageRaw = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetCurrentColorization);
            var response = ApiResponse.FromString(messageRaw);

            if (response.StatusCode == StatusCode.Ok && !response.Message.Equals(initial, StringComparison.CurrentCultureIgnoreCase))
            {
                break;
            }

            await Task.Delay(1000);
        }
    }

    partial void OnIsColorizationEnabledChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _builder.Config.ColorizationSwitch.Enabled = value;

        SafeSaveBuilder();
    }

    partial void OnSelectColorThemeModeChanged(ApplicationTheme value)
    {
        if (_isInitializing)
        {
            return;
        }

        LoadSettings();
    }

    partial void OnAccentColorModeChanged(ThemeColorMode value)
    {
        if (_isInitializing)
        {
            return;
        }

        var isLightTheme = SelectColorThemeMode == ApplicationTheme.Light;
        var config = _builder.Config.ColorizationSwitch.Component;
        var isAutomatic = value == ThemeColorMode.Automatic;

        DesktopPreviewAccentColorBorderVisible = !isAutomatic;

        if (isLightTheme)
        {
            config.LightAutoColorization = isAutomatic;
        }
        else
        {
            config.DarkAutoColorization = isAutomatic;
        }

        SafeSaveBuilder();

        try
        {
            RequestThemeSwitch();

            if (isAutomatic)
            {
                var hexColor = (isLightTheme ? config.LightHex : config.DarkHex).ToLower();
                Task.Run(async () => await WaitForColorizationChange(hexColor, 5));
            }

            InitializeAccentColor();
            LoadSettings();
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationViewModel");
        }
    }
}
