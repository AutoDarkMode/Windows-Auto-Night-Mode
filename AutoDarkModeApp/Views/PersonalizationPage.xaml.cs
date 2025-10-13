using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class PersonalizationPage : Page
{
    public PersonalizationViewModel ViewModel { get; }

    public PersonalizationPage()
    {
        ViewModel = App.GetService<PersonalizationViewModel>();
        InitializeComponent();
    }

    private void PickWallpaperSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigation = App.GetService<INavigationService>();
        if (navigation?.Frame != null)
        {
            navigation.RegisterCustomBreadcrumbBarItem(
                typeof(WallpaperPickerViewModel).FullName!,
                new Services.BreadcrumbItem() { Content = "Background".GetLocalized(), Tag = "WallpaperPicker" }
            );
            navigation.NavigateTo(typeof(WallpaperPickerViewModel).FullName!);
        }
    }

    private void ColorizationPickSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigation = App.GetService<INavigationService>();
        if (navigation?.Frame != null)
        {
            navigation.RegisterCustomBreadcrumbBarItem(
                typeof(ColorizationViewModel).FullName!,
                new Services.BreadcrumbItem() { Content = "AccentColor".GetLocalized(), Tag = "Colorization" }
            );
            navigation.NavigateTo(typeof(ColorizationViewModel).FullName!);
        }
    }

    private void CursorsSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigation = App.GetService<INavigationService>();
        if (navigation?.Frame != null)
        {
            navigation.RegisterCustomBreadcrumbBarItem(
                typeof(CursorsViewModel).FullName!,
                new Services.BreadcrumbItem() { Content = "Cursors".GetLocalized(), Tag = "Cursors" }
            );
            navigation.NavigateTo(typeof(CursorsViewModel).FullName!);
        }
    }

    private void ThemePickerSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigation = App.GetService<INavigationService>();
        if (navigation?.Frame != null)
        {
            navigation.RegisterCustomBreadcrumbBarItem(
                typeof(ThemePickerViewModel).FullName!,
                new Services.BreadcrumbItem() { Content = "Theme".GetLocalized(), Tag = "ThemePicker" }
            );
            navigation.NavigateTo(typeof(ThemePickerViewModel).FullName!);
        }
    }
}
