using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Views;

public sealed partial class SystemAreasPage : Page
{
    public SystemAreasViewModel ViewModel { get; }

    public SystemAreasPage()
    {
        ViewModel = App.GetService<SystemAreasViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
