using System.Diagnostics;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
        //this.DataContext = new LanguageViewModel();
    }

    private void OpenConfigSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "config.yaml");
        new Process { StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true } }.Start();
    }

    private void OpenConfigFolderSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
        new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c start " + folderPath,
                WindowStyle = ProcessWindowStyle.Hidden,
            },
        }.Start();
    }
}
