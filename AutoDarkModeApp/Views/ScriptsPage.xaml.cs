using AutoDarkModeApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class ScriptsPage : Page
{
    public ScriptsViewModel ViewModel
    {
        get;
    }

    public ScriptsPage()
    {
        ViewModel = App.GetService<ScriptsViewModel>();
        InitializeComponent();
    }
}
