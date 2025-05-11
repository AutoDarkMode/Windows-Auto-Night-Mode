using System.Diagnostics;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class ScriptsPage : Page
{
    public ScriptsViewModel ViewModel { get; }

    public ScriptsPage()
    {
        ViewModel = App.GetService<ScriptsViewModel>();
        InitializeComponent();
    }

    private void OpenScriptConfigSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "scripts.yaml");
        new Process
        {
            StartInfo = new ProcessStartInfo(filepath)
            {
                UseShellExecute = true
            }
        }.Start();
    }
}
