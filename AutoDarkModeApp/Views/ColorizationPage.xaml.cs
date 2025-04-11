using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
        CheckColorColorPicker.Color = _builder.Config.ColorizationSwitch.Component.LightHex.ToColor();
        var result = await ColorPickerContentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _builder.Config.ColorizationSwitch.Component.LightHex = CheckColorColorPicker.Color.ToHex();
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
        CheckColorColorPicker.Color = _builder.Config.ColorizationSwitch.Component.DarkHex.ToColor();
        var result = await ColorPickerContentDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _builder.Config.ColorizationSwitch.Component.DarkHex = CheckColorColorPicker.Color.ToHex();
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

    protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.OnViewModelNavigatedFrom(e);
}
