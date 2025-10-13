using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.Views;

public sealed partial class ColorizationPage : Page
{
    private readonly IErrorService _errorService = App.GetService<IErrorService>();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly AdmConfigBuilder _builder = AdmConfigBuilder.Instance();

    public ColorizationViewModel ViewModel { get; }

    public ColorizationPage()
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        ViewModel = App.GetService<ColorizationViewModel>();
        InitializeComponent();
    }

    private async void LightModeCheckColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl();
        dialogContent.InternalColorPicker.IsAlphaEnabled = false;
        dialogContent.InternalColorPicker.Color = _builder.Config.ColorizationSwitch.Component.LightHex.ToColor();
        var colorPickerDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "SelectColor".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };
        var result = await colorPickerDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.LightModeColorPreviewBorderBackground = new SolidColorBrush(dialogContent.InternalColorPicker.Color);
            _builder.Config.ColorizationSwitch.Component.LightHex = dialogContent.InternalColorPicker.Color.ToHex();

            try
            {
                _builder.Save();
                if (_builder.Config.ColorizationSwitch.Enabled) _dispatcherQueue.TryEnqueue(() => RequestThemeSwitch());
            }
            catch (Exception ex)
            {
                await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
            }
        }
    }

    private async void DarkModeCheckColorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl();
        dialogContent.InternalColorPicker.IsAlphaEnabled = false;
        dialogContent.InternalColorPicker.Color = _builder.Config.ColorizationSwitch.Component.DarkHex.ToColor();
        var colorPickerDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = "SelectColor".GetLocalized(),
            CloseButtonText = "Cancel".GetLocalized(),
            PrimaryButtonText = "Save".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
        };
        var result = await colorPickerDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.DarkModeColorPreviewBorderBackground = new SolidColorBrush(dialogContent.InternalColorPicker.Color);
            _builder.Config.ColorizationSwitch.Component.DarkHex = dialogContent.InternalColorPicker.Color.ToHex();
            try
            {
                _builder.Save();
                if (_builder.Config.ColorizationSwitch.Enabled) _dispatcherQueue.TryEnqueue(() => RequestThemeSwitch());
            }
            catch (Exception ex)
            {
                await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
            }
        }

    }

    private async void RequestThemeSwitch()
    {
        try
        {
            var result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
            if (result != StatusCode.Ok)
            {
                throw new SwitchThemeException(result, "ColorizationPage");
            }
        }
        catch (Exception ex)
        {
            await _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "ColorizationPage");
        }
    }
}
