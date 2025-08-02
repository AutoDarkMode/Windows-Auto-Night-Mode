using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.Views;

public sealed partial class ColorizationPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public ColorizationViewModel ViewModel { get; }

    public ColorizationPage()
    {
        ViewModel = App.GetService<ColorizationViewModel>();
        InitializeComponent();
    }

    private async void LightModeCheckColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl() { CustomColor = _builder.Config.ColorizationSwitch.Component.LightHex.ToColor() };
        var colorPickerDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "ChooseCustomColor".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };
        var result = await colorPickerDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.LightModeColorPreviewBorderBackground = new SolidColorBrush(dialogContent.CustomColor);
            _builder.Config.ColorizationSwitch.Component.LightHex = dialogContent.CustomColor.ToHex();
        }

        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
        }
    }

    private async void DarkModeCheckColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl() { CustomColor = _builder.Config.ColorizationSwitch.Component.DarkHex.ToColor() };
        var colorPickerDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "ChooseCustomColor".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };
        var result = await colorPickerDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.DarkModeColorPreviewBorderBackground = new SolidColorBrush(dialogContent.CustomColor);
            _builder.Config.ColorizationSwitch.Component.DarkHex = dialogContent.CustomColor.ToHex();
        }
        try
        {
            _builder.Save();
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
        }
    }
}
