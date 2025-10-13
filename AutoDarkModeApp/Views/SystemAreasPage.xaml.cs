using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class SystemAreasPage : Page
{
    public SystemAreasViewModel ViewModel { get; }

    public SystemAreasPage()
    {
        ViewModel = App.GetService<SystemAreasViewModel>();
        InitializeComponent();
    }

    private async void DarkReaderWebBrowserHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser"));
    }

    private async void WindowsColorsSettingHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
    }
}
