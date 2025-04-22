using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial object? Selected { get; set; }

    public INavigationService NavigationService { get; }

    public MainViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationService.SettingsItem;
            return;
        }

        var selectedItem = NavigationService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
