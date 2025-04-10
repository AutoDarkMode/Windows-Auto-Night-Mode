using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Windowing;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : WindowEx
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTitleBarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        Title = Debugger.IsAttached ? "Auto Dark Mode Dev" : "Auto Dark Mode";

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationService.InitializeNavigationView(NavigationViewControl);

        RootGrid.KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        RootGrid.KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
#if DEBUG
        TitleBar.Subtitle = "Debug";
#endif

        Closed += MainWindow_Closed;
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

    private KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();
        var result = navigationService.GoBack();
        args.Handled = result;
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
