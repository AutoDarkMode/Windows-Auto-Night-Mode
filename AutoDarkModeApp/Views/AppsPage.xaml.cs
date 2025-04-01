using AutoDarkModeApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class AppsPage : Page
{
    public AppsViewModel ViewModel
    {
        get;
    }

    public AppsPage()
    {
        ViewModel = App.GetService<AppsViewModel>();
        InitializeComponent();
    }
}
