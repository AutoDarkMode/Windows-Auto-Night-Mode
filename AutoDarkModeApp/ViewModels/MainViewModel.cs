using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Views;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    // TODO: Waiting for PR merge https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/pull/933

    //[ObservableProperty]
    //public bool IsBackEnabled;

    //[ObservableProperty]
    //public object? Selecte;

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    public INavigationService NavigationService { get; }

    public MainViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

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