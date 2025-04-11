using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace AutoDarkModeApp.Views;

public sealed partial class WallpaperPickerPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public WallpaperPickerViewModel ViewModel { get; }

    public WallpaperPickerPage()
    {
        ViewModel = App.GetService<WallpaperPickerViewModel>();
        InitializeComponent();
    }

    private void GlobalWallpaperPathHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if(ViewModel.GlobalWallpaperPath != null)
        {
            WallpaperPreviewImage.Source = new BitmapImage(new Uri(ViewModel.GlobalWallpaperPath)); ;
        }
        WallpaperPreviewTeachingTip.IsOpen = true;
    }

    private async void SetColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ChooseColorPicker.Color = ViewModel.SelectWallpaperThemeMode == Microsoft.UI.Xaml.ApplicationTheme.Light
            ? _builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor()
            : _builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor();

        var result = await ColorPickerContentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (ViewModel.SelectWallpaperThemeMode == Microsoft.UI.Xaml.ApplicationTheme.Light)
            {
                _builder.Config.WallpaperSwitch.Component.SolidColors.Light = ChooseColorPicker.Color.ToHex();
            }
            else
            {
                _builder.Config.WallpaperSwitch.Component.SolidColors.Dark = ChooseColorPicker.Color.ToHex();
            }
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "WallpaperPickerPage");
        }
    }

    private async void WindowsSpotlightHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:personalization-background"));
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
