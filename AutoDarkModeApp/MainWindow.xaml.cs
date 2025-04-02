using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private UISettings settings;

    public MainViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTitleBarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));

        Title = Debugger.IsAttached ? "Auto Dark Mode Dev" : "Auto Dark Mode";

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(TitleBar);
#if DEBUG
        TitleBar.Subtitle = "Dev";
#endif
        Closed += MainWindow_Closed;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    }

    private void NavViewTitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
        }
    }

    private void NavViewTitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
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

        Close();

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
