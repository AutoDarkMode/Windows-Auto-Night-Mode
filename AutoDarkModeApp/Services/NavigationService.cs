using System.Diagnostics.CodeAnalysis;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Services;

public class NavigationService : INavigationService
{
    private object? _lastParameterUsed;
    private Frame? _frame;
    private NavigationView? _navigationView;

    public event NavigatedEventHandler? Navigated;

    public IList<object>? MenuItems => _navigationView?.MenuItems;
    public object? SettingsItem => _navigationView?.SettingsItem;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = App.MainWindow.Content as Frame;
                RegisterFrameEvents();
            }
            return _frame;
        }
        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public void InitializeNavigationView(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.SelectionChanged += OnSelectionChanged;
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        if (_frame == null)
            return false;

        var pageType = Type.GetType(pageKey);
        if (pageType == null)
            return false;

        if (_frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(_lastParameterUsed)))
        {
            _frame.Tag = clearNavigation;
            var navigated = _frame.Navigate(pageType, parameter);
            if (navigated)
            {
                _lastParameterUsed = parameter;
            }
            return navigated;
        }
        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            Navigated?.Invoke(sender, e);
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateTo(typeof(SettingsPage).FullName!);
        }
        else if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var pageKey = selectedItem.Tag?.ToString();
            if (!string.IsNullOrEmpty(pageKey))
            {
                NavigateTo(pageKey);
            }
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            var pageTypeName = pageType.FullName;
            return FindItemByPageType(_navigationView.MenuItems, pageTypeName) ?? FindItemByPageType(_navigationView.FooterMenuItems, pageTypeName);
        }
        return null;
    }

    private NavigationViewItem? FindItemByPageType(IEnumerable<object> menuItems, string pageTypeName)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (item.Tag?.ToString() == pageTypeName)
            {
                return item;
            }

            var foundInChildren = FindItemByPageType(item.MenuItems, pageTypeName);
            if (foundInChildren != null)
            {
                return foundInChildren;
            }
        }
        return null;
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
