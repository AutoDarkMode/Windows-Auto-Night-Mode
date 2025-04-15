using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Views;

public sealed partial class AppsPage : Page
{
    public AppsViewModel ViewModel { get; }

    public AppsPage()
    {
        ViewModel = App.GetService<AppsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
