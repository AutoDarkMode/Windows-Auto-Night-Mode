using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Services;

public class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private Frame? _frame;
    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;
    public object? SettingsItem => _navigationView?.SettingsItem;

    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    public NavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public void InitializeNavigationView(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.SelectionChanged += OnSelectionChanged;

        if (_navigationView.MenuItems.Count > 0)
        {
            var firstItem = _navigationView.MenuItems[0] as NavigationViewItem;
            _navigationView.SelectedItem = firstItem;

            if (firstItem?.Tag is string pageKey)
            {
                NavigateTo(pageKey);
            }
        }
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        if (_frame == null)
            return false;

        var pageType = _pageService.GetPageType(pageKey);

        if (clearNavigation)
        {
            _frame.BackStack.Clear();
        }

        return _frame.Navigate(pageType, parameter);
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateTo(typeof(SettingsViewModel).FullName!);
            if (args.SelectedItemContainer is NavigationViewItem item && item.Content is string content)
            {
                _navigationView!.Header = content;
            }
        }
        else if (args.SelectedItemContainer?.Tag is string pageKey)
        {
            NavigateTo(pageKey);
            if (args.SelectedItemContainer is NavigationViewItem item && item.Content is string content)
            {
                _navigationView!.Header = content;
            }
        }
    }
}
