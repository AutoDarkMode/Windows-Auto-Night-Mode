using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.WinUI.Helpers;
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

        DispatcherQueue.TryEnqueue(LoadMonitors);
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

    private async void RemoveDisconnectedMonitorsHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private async void CheckColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckColorColorPicker.Color =
            ViewModel.SelectWallpaperThemeMode == Microsoft.UI.Xaml.ApplicationTheme.Light
                ? _builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor()
                : _builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor();

        var result = await ColorPickerContentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (ViewModel.SelectWallpaperThemeMode == Microsoft.UI.Xaml.ApplicationTheme.Light)
            {
                _builder.Config.WallpaperSwitch.Component.SolidColors.Light = CheckColorColorPicker.Color.ToHex();
            }
            else
            {
                _builder.Config.WallpaperSwitch.Component.SolidColors.Dark = CheckColorColorPicker.Color.ToHex();
            }
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    private async void WindowsSpotlightHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:personalization-background"));
    }
}
