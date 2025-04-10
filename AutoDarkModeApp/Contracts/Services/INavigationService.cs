using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Contracts.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack { get; }
    Frame? Frame { get; set; }
    IList<object>? MenuItems { get; }
    object? SettingsItem { get; }

    void InitializeNavigationView(NavigationView navigationView);
    NavigationViewItem? GetSelectedItem(Type pageType);
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);
    bool GoBack();
}