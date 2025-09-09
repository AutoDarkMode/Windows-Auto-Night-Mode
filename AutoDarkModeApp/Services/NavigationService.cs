using System.Collections.ObjectModel;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace AutoDarkModeApp.Services;

public class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private Frame? _frame;
    private NavigationView? _navigationView;
    private BreadcrumbBar? _breadcrumbBar;

    private readonly ObservableCollection<BreadcrumbItem> _breadcrumbItems = [];
    private readonly Dictionary<Type, string> _customHeaders = [];
    private readonly Dictionary<Type, string> _hiddenPageParents = new()
{
    // Replace with your actual page types
    { typeof(WallpaperPickerPage), "Personalization" },
    { typeof(ColorizationPage), "Personalization" },
    { typeof(CursorsPage), "Personalization" },
    { typeof(ThemePickerPage), "Personalization" }
};

    public IList<object>? MenuItems => _navigationView?.MenuItems;
    public object? SettingsItem => _navigationView?.SettingsItem;

    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    // Please do not use Primary Constructors here
    public NavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public void InitializeNavigationView(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.SelectionChanged += OnSelectionChanged;

        _navigationView.PointerPressed += NavigationView_PointerPressed;
        _navigationView.KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        _navigationView.KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Menu));
        _navigationView.KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
        _navigationView.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;

        if (_frame != null)
        {
            _frame.Navigating += OnNavigating;
            _frame.Navigated += OnNavigated;
        }
    }

    public void InitializeBreadcrumbBar(BreadcrumbBar breadcrumbBar)
    {
        _breadcrumbBar = breadcrumbBar;
        _breadcrumbBar.ItemsSource = _breadcrumbItems;
        _breadcrumbBar.ItemClicked += OnBreadcrumbItemClicked;
    }

    public void RegisterCustomHeader(string key, string header)
    {
        var pageType = _pageService.GetPageType(key);
        _customHeaders[pageType] = header;
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
        }
        else if (args.SelectedItemContainer?.Tag is string pageKey)
        {
            pageKey = "AutoDarkModeApp.ViewModels." + pageKey + "ViewModel";
            NavigateTo(pageKey);
        }
    }

    private KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_frame == null)
        {
            args.Handled = false;
            return;
        }

        if (sender.Key == VirtualKey.Left && sender.Modifiers == VirtualKeyModifiers.Menu)
        {
            if (_frame.CanGoBack)
            {
                _frame.GoBack();
                args.Handled = true;
            }
            else
            {
                args.Handled = false;
            }
        }
        else if (sender.Key == VirtualKey.Right && sender.Modifiers == VirtualKeyModifiers.Menu)
        {
            if (_frame.CanGoForward)
            {
                _frame.GoForward();
                args.Handled = true;
            }
            else
            {
                args.Handled = false;
            }
        }
        else
        {
            args.Handled = false;
        }
    }

    private void NavigationView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;

        if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
            return;

        if (_frame != null)
        {
            if (properties.IsXButton1Pressed && _frame.CanGoBack)
            {
                _frame.GoBack();
                e.Handled = true;
            }
            else if (properties.IsXButton2Pressed && _frame.CanGoForward)
            {
                _frame.GoForward();
                e.Handled = true;
            }
        }
    }

    private List<NavigationViewItem> GetFullNavigationPath(Type pageType)
    {
        var path = GetNavigationPath(_navigationView.MenuItems, pageType);
        if (path.Count > 0)
            return path;

        // Check footer menu items
        path = GetNavigationPath(_navigationView.FooterMenuItems, pageType);
        if (path.Count > 0)
            return path;

        // Check if it's the Settings item
        if (_navigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            if (IsMenuItemForPageType(settingsItem, pageType))
                return [settingsItem];
        }

        return [];
    }

    // Replace in OnNavigated:
    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (_navigationView == null)
            return;

        // Get the path to the loaded page in the nav menu, including footer and settings
        var path = GetFullNavigationPath(e.SourcePageType);

        // Optionally update breadcrumbs
        _breadcrumbItems.Clear();
        if (path.Count > 0)
        {
            foreach (var item in path)
                _breadcrumbItems.Add(new BreadcrumbItem { Content = item.Content, Tag = item.Tag });
        }
        else if (_hiddenPageParents.TryGetValue(e.SourcePageType, out var parentName))
        {
            // Add the parent as the first breadcrumb
            _breadcrumbItems.Add(new BreadcrumbItem { Content = parentName, Tag = "Personalization" });
            // Add the current page as the second breadcrumb
            _breadcrumbItems.Add(new BreadcrumbItem { Content = e.SourcePageType.Name, Tag = e.SourcePageType.Name });
        }
    }

    private void OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        StateUpdateHandler.ClearAllEvents();
    }

    private void OnBreadcrumbItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is BreadcrumbItem breadcrumbItem && breadcrumbItem.Tag is string pageKey)
        {
            pageKey = "AutoDarkModeApp.ViewModels." + pageKey + "ViewModel";
            NavigateTo(pageKey);
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.Tag is string pageKey)
        {
            pageKey = "AutoDarkModeApp.ViewModels." + pageKey + "ViewModel";
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }
        return false;
    }

    private List<NavigationViewItem> GetNavigationPath(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return [item];
            }

            var childPath = GetNavigationPath(item.MenuItems, pageType);
            if (childPath.Count > 0)
            {
                var path = new List<NavigationViewItem> { item };
                path.AddRange(childPath);
                return path;
            }
        }
        return [];
    }
}

public class BreadcrumbItem
{
    public object? Content { get; set; }
    public object? Tag { get; set; }
}
