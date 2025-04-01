using AutoDarkModeApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class DonationPage : Page
{
    public DonationViewModel ViewModel
    {
        get;
    }

    public DonationPage()
    {
        ViewModel = App.GetService<DonationViewModel>();
        InitializeComponent();
    }
}
