using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace AutoDarkModeApp;

public sealed partial class MainWindow : WindowEx
{
    public ShellViewModel ViewModel { get; set; }

    public MainWindow(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));
        AppWindow.SetTitleBarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AutoDarkModeIcon.ico"));

        // TODO: Set the title bar icon by updating /Assets/WindowIcon.ico.
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        Title = Debugger.IsAttached ? "Auto Dark Mode Dev" : "Auto Dark Mode";

#if DEBUG
        TitleBar.Subtitle = "Dev";
#endif

        Closed += MainWindow_Closed;
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
        //Debug.WriteLine("Save size: " + postion.X + "\t" + postion.Y + "\t" + bounds.Width + "\t" + bounds.Height);

        // TODO: Maybe we could delete it `Close()`. The reason why `Process.GetCurrentProcess().Kill();` is used, it because MapLocationFinder;
        //Close();

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

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Add handler for ContentFrame navigation.
        NavigationFrame.Navigated += On_Navigated;

        // NavView doesn't load any page by default, so load home page.
        NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        // If navigation occurs on SelectionChanged, this isn't needed.
        // Because we use ItemInvoked to navigate, we need to call Navigate
        // here to load the home page.
        NavView_Navigate(typeof(TimePage), new EntranceNavigationTransitionInfo());

    }

    private void NavigationViewControl_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked == true)
        {
            NavView_Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
        }
        else if (args.InvokedItemContainer != null)
        {
            Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
            NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
        }
    }
    // NavView_SelectionChanged is not used.
    // You will typically handle either ItemInvoked or SelectionChanged to perform navigation,
    // but not both.

    private void NavView_Navigate(
        Type navPageType,
        NavigationTransitionInfo transitionInfo)
    {
        // Get the page type before navigation so you can prevent duplicate
        // entries in the backstack.
        Type preNavPageType = NavigationFrame.CurrentSourcePageType;

        // Only navigate if the selected page isn't currently loaded.
        if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
        {
            NavigationFrame.Navigate(navPageType, null, transitionInfo);
        }
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

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        NavigationViewControl.IsBackEnabled = NavigationFrame.CanGoBack;

        if (NavigationFrame.SourcePageType == typeof(SettingsPage))
        {
            // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
            NavigationViewControl.SelectedItem = (NavigationViewItem)NavigationViewControl.SettingsItem;
            NavigationViewControl.Header = "Settings";
        }
        else if (NavigationFrame.SourcePageType != null)
        {
            // Select the nav view item that corresponds to the page being navigated to.
            //NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems
            //    .OfType<NavigationViewItem>()
            //    .FirstOrDefault(i => i.Tag?.Equals(NavigationFrame.SourcePageType?.FullName?.ToString()) == true);

            if (NavigationViewControl.SelectedItem != null)
            {
                NavigationViewControl.Header = ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content?.ToString();
            }
            else
            {
                // Handle the case where no matching item is found
                NavigationViewControl.Header = "Unknown Page";
            }
        }
    }

}
