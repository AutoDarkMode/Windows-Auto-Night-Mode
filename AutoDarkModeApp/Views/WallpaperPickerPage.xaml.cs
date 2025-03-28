using AutoDarkModeApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class WallpaperPickerPage : Page
{
    public WallpaperPickerViewModel ViewModel
    {
        get;
    }

    public WallpaperPickerPage()
    {
        ViewModel = App.GetService<WallpaperPickerViewModel>();
        InitializeComponent();
    }
}
