using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Services;

// Thanks to Jay for providing the code to update the related functions of the parent page

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();
    private readonly Dictionary<string, Type> _pageParents = new();

    public PageService()
    {
        Configure<TimeViewModel, TimePage>();
        Configure<ConditionsViewModel, ConditionsPage>();
        Configure<HotkeysViewModel, HotkeysPage>();
        Configure<SystemAreasViewModel, SystemAreasPage>();
        Configure<PersonalizationViewModel, PersonalizationPage>();
        Configure<ScriptsViewModel, ScriptsPage>();
        Configure<DonationViewModel, DonationPage>();
        Configure<AboutViewModel, AboutPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<WallpaperPickerViewModel, WallpaperPickerPage>();
        Configure<ColorizationViewModel, ColorizationPage>();
        Configure<CursorsViewModel, CursorsPage>();
        Configure<ThemePickerViewModel, ThemePickerPage>();

        Matching<WallpaperPickerPage, PersonalizationPage>();
        Matching<ColorizationPage, PersonalizationPage>();
        Matching<CursorsPage, PersonalizationPage>();
        Matching<ThemePickerPage, PersonalizationPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    public Type? GetPageParents(string key)
    {
        Type? pageType;
        lock (_pageParents)
        {
            _pageParents.TryGetValue(key, out pageType);
        }

        return pageType;
    }

    public List<Type> GetPageParentChain(string key)
    {
        var parentChain = new List<Type>();
        var currentPageType = GetPageType(key);

        while (currentPageType != null)
        {
            var parentType = GetPageParents(currentPageType.FullName!);
            if (parentType != null)
            {
                parentChain.Insert(0, parentType);
                currentPageType = parentType;
            }
            else
            {
                break;
            }
        }

        return parentChain;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }

    private void Matching<CV, PV>()
        where CV : Page
        where PV : Page
    {
        lock (_pageParents)
        {
            var key = typeof(CV).FullName!;
            if (_pageParents.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already matched in PageService");
            }

            var type = typeof(PV);

            _pageParents.Add(key, type);
        }
    }
}
