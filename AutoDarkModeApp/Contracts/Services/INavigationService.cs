using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Contracts.Services;

public interface INavigationService
{
    Frame? Frame { get; set; }
    IList<object>? MenuItems { get; }
    object? SettingsItem { get; }

    void InitializeNavigationView(NavigationView navigationView);
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);
    void RegisterCustomHeader(string key, string header);
}
