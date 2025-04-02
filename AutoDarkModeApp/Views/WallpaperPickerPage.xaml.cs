using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class WallpaperPickerPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public WallpaperPickerViewModel ViewModel
    {
        get;
    }

    public WallpaperPickerPage()
    {
        ViewModel = App.GetService<WallpaperPickerViewModel>();
        InitializeComponent();
    }

    private void GlobalWallpaperPathHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filepath = (sender as HyperlinkButton)?.Content?.ToString();
        if (!string.IsNullOrEmpty(filepath))
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(filepath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch (Exception ex)
            {
                _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AboutPage");
            }
        }
    }

    private async void SetColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.SelectIndexWallpaperMode == 0)
        {
            ChooseColorPicker.Color = _builder.Config.WallpaperSwitch.Component.SolidColors.Light.ToColor();
        }
        else
        {
            ChooseColorPicker.Color = _builder.Config.WallpaperSwitch.Component.SolidColors.Dark.ToColor();
        }

        var result = await ColorPickerContentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (ViewModel.SelectIndexWallpaperMode == 0)
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
}
