using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class WallpaperPickerPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public WallpaperPickerViewModel ViewModel { get; }

    public WallpaperPickerPage()
    {
        ViewModel = App.GetService<WallpaperPickerViewModel>();
        InitializeComponent();

        if (_builder.Config.WallpaperSwitch.Component.Monitors.Count == 0)
            Task.Run(async () => await DetectMonitorsAsync());

        DispatcherQueue.TryEnqueue(LoadMonitors);
    }

    private async Task DetectMonitorsAsync()
    {
        try
        {
            string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.DetectMonitors);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "WallpaperPickerPage");
            }
            try
            {
                _builder.Load();
            }
            catch (Exception ex)
            {
                await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "Constructor");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "Constructor");
        }
    }

    private void LoadMonitors()
    {
        // Generate a list with all installed Monitors, select the first one
        List<MonitorSettings> monitors = _builder.Config.WallpaperSwitch.Component.Monitors;
        var disconnected = new List<MonitorSettings>();
        var connected = monitors
            .Where(m =>
            {
                // Preload tostring to avoid dropdown opening lag
                m.ToString();
                // Return monitors connecte to system connected monitors
                if (m.Connected)
                {
                    return true;
                }
                disconnected.Add(m);
                return false;
            })
            .ToList();

        foreach (var monitor in disconnected)
        {
            monitor.MonitorString = $"{"Disconnected".GetLocalized()} - {monitor.MonitorString}";
        }

        monitors.Clear();
        monitors.AddRange(connected);
        monitors.AddRange(disconnected);

        MonitorsComboBox.ItemsSource = monitors;
        ViewModel.SelectMonitor = monitors.FirstOrDefault();
    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "WallpaperPickerPage");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    private async void RemoveDisconnectedMonitorsHyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CleanMonitors);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException($"Couldn't clean up monitor list, {result}", "WallpaperPickerPage");
            }
            try
            {
                _builder.Load();
                List<MonitorSettings> monitors = _builder.Config.WallpaperSwitch.Component.Monitors;
                MonitorsComboBox.ItemsSource = monitors;
                MonitorsComboBox.SelectedItem = monitors.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CleanMonitors");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CleanMonitors");
        }
    }

    private async void CheckColorButton_Click(object sender, RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl();
        dialogContent.InternalColorPicker.IsAlphaEnabled = false;
        dialogContent.InternalColorPicker.Color =
            ViewModel.SelectWallpaperThemeMode == ApplicationTheme.Light
                ? _builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor()
                : _builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor();
        var colorPickerDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "SelectColor".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Set".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };
        var result = await colorPickerDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var color = dialogContent.InternalColorPicker.Color;
            var rgbColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            if (ViewModel.SelectWallpaperThemeMode == ApplicationTheme.Light)
                _builder.Config.WallpaperSwitch.Component.SolidColors.Light = rgbColor;
            else
                _builder.Config.WallpaperSwitch.Component.SolidColors.Dark = rgbColor;

            ViewModel.ColorPreviewBorderBackground = new Microsoft.UI.Xaml.Media.SolidColorBrush(dialogContent.InternalColorPicker.Color);
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }

        DispatcherQueue.TryEnqueue(() => RequestThemeSwitch());
    }

    private async void WindowsSpotlightHyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:personalization-background"));
    }
}
