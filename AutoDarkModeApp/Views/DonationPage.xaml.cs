using System.Diagnostics;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class DonationPage : Page
{
    public DonationViewModel ViewModel { get; }

    public DonationPage()
    {
        ViewModel = App.GetService<DonationViewModel>();
        InitializeComponent();
    }

    private void DonationPayPalButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartProcessByProcessInfo("https://www.paypal.com/donate/?hosted_button_id=H65KZYMHKCB6E");
    }

    private void GithubSponsorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartProcessByProcessInfo("https://github.com/sponsors/Spiritreader");
    }

    private static void StartProcessByProcessInfo(string message)
    {
        Process.Start(new ProcessStartInfo(message) { UseShellExecute = true, Verb = "open" });
    }
}
