using System.Diagnostics;
using AutoDarkModeApp.Helpers;
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

        ThemeTutorialFirstStepInfoBar.Title = "Step".GetLocalized() + " 1";
        ThemeTutorialSecondStepInfoBar.Title = "Step".GetLocalized() + " 2";
        ThemeTutorialThirdStepInfoBar.Title = "Step".GetLocalized() + " 3";

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

    private async void OpenWindowsThemePageHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:themes"));
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
