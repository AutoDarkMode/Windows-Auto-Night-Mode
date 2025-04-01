using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        Content = null;

        if (Debugger.IsAttached)
        {
            Title = "Auto Dark Mode Dev";
        }
        else
        {
            Title = "Auto Dark Mode";
        }

        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        var postion = App.MainWindow.AppWindow.Position;
        var size = App.MainWindow.AppWindow.Size;
        var localSettings = App.GetService<ILocalSettingsService>();
        await Task.Run(async () =>
        {
            await localSettings.SaveSettingAsync("X", postion.X);
            await localSettings.SaveSettingAsync("Y", postion.Y);
            await localSettings.SaveSettingAsync("Width", size.Width);
            await localSettings.SaveSettingAsync("Height", size.Height);
        });

        //TODO: MapLocationFinder will make WinUI app hang on exit, more information on https://github.com/microsoft/microsoft-ui-xaml/issues/10229
        Close();
        try
        {
            Process.GetCurrentProcess().Kill();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
