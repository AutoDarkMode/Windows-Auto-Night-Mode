using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class TimePage : Page
{
    private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

    public TimeViewModel ViewModel
    {
        get;
    }

    public TimePage()
    {
        ViewModel = App.GetService<TimeViewModel>();
        InitializeComponent();
    }
}

