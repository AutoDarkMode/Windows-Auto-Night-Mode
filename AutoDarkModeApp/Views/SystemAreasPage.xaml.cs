using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class SystemAreasPage : Page
{
    public SystemAreasViewModel ViewModel { get; }

    public SystemAreasPage()
    {
        ViewModel = App.GetService<SystemAreasViewModel>();
        InitializeComponent();
    }
}
