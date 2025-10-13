using System.Diagnostics;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class ThemePickerPage : Page
{
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public ThemePickerViewModel ViewModel { get; }

    public ThemePickerPage()
    {
        ViewModel = App.GetService<ThemePickerViewModel>();
        InitializeComponent();

        DispatcherQueue.TryEnqueue(() => LoadThemes());
    }

    private void LoadThemes()
    {
        List<ThemeFile> themeCollection = ThemeCollectionHandler.GetUserThemes();
        IEnumerable<string> themeNames = themeCollection.Select(t => t.ToString()).ToList();
        LightThemeComboBox.ItemsSource = themeNames;
        DarkThemeComboBox.ItemsSource = themeNames;
        ThemeFile? lightSelected = themeCollection.FirstOrDefault(t => t.Path == _builder.Config.WindowsThemeMode.LightThemePath);
        ThemeFile? darkSelected = themeCollection.FirstOrDefault(t => t.Path == _builder.Config.WindowsThemeMode.DarkThemePath);
        if (lightSelected != null)
        {
            ViewModel.SelectedLightTheme = lightSelected.ToString();
        }

        if (darkSelected != null)
        {
            ViewModel.SelectedDarkTheme = darkSelected.ToString();
        }
    }

    private void OpenThemeFolderSettingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var themeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
        try
        {
            Process.Start(new ProcessStartInfo(themeFolderPath) { UseShellExecute = true, Verb = "open" });
        }
        catch
        {
            Directory.CreateDirectory(themeFolderPath);
            Process.Start(new ProcessStartInfo(themeFolderPath) { UseShellExecute = true, Verb = "open" });
        }
    }
}
