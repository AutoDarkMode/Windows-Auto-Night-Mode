using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class PersonalizationPage : Page
{
    public PersonalizationViewModel ViewModel
    {
        get;
    }

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
            navigation.NavigateTo(typeof(WallpaperPickerViewModel).FullName!);
        }
    }
}
