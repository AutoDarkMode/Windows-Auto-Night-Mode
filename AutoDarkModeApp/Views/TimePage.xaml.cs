using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class TimePage : Page
{
    private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

    public TimeViewModel ViewModel
    {
        get;
    }

    public TimePage()
    {
        ViewModel = App.GetService<TimeViewModel>();
        InitializeComponent();
    }

    private async void NightLightSettingsCard_Clicked(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:nightlight"));
    }

    private async void GetCoordinatesSettingsCard_Clicked(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.latlong.net/"));
    }
}

