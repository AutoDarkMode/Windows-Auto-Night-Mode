using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

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

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
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
        //Debug.WriteLine("Save size: " + postion.X + "\t" + postion.Y + "\t" + bounds.Width + "\t" + bounds.Height);

        //Debug.WriteLine("Will kill process");
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
