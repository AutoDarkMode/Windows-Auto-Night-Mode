using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AutoDarkModeApp.Views;

public sealed partial class ColorizationPage : Page
{
    public ColorizationViewModel ViewModel { get; }

    public ColorizationPage()
    {
        ViewModel = App.GetService<ColorizationViewModel>();
        InitializeComponent();
    }

    private void WindowsColorGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var rectangleItem = e.ClickedItem as Rectangle;
        var solidColorBrush = rectangleItem?.Fill as SolidColorBrush;
        if (solidColorBrush == null)
        {
            return;
        }
        var rectangleItemFill = solidColorBrush.Color;
        ViewModel.AccentColorPreviewBorderBackground = new SolidColorBrush(rectangleItemFill);
        if (ViewModel.SelectColorThemeMode == ApplicationTheme.Light)
        {
            ViewModel.GetAdmConfig().ColorizationSwitch.Component.LightHex = rectangleItemFill.ToHex();
        }
        else
        {
            ViewModel.GetAdmConfig().ColorizationSwitch.Component.DarkHex = rectangleItemFill.ToHex();
        }
        ViewModel.AccentColorMode = ColorizationViewModel.ThemeColorMode.Manual;

        ViewModel.SafeSaveBuilder();
        if (ViewModel.GetAdmConfig().ColorizationSwitch.Enabled)
        {
            ViewModel.RequestThemeSwitch();
        }
    }

    private async void CheckColorButton_Click(object sender, RoutedEventArgs e)
    {
        var dialogContent = new ColorPickerDialogContentControl();
        dialogContent.InternalColorPicker.IsAlphaEnabled = false;
        if (ViewModel.SelectColorThemeMode == ApplicationTheme.Light)
        {
            dialogContent.InternalColorPicker.Color = ViewModel.GetAdmConfig().ColorizationSwitch.Component.LightHex.ToColor();
        }
        else
        {
            dialogContent.InternalColorPicker.Color = ViewModel.GetAdmConfig().ColorizationSwitch.Component.DarkHex.ToColor();
        }
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
            ViewModel.AccentColorPreviewBorderBackground = new SolidColorBrush(dialogContent.InternalColorPicker.Color);
            if (ViewModel.SelectColorThemeMode == ApplicationTheme.Light)
            {
                ViewModel.GetAdmConfig().ColorizationSwitch.Component.LightHex = dialogContent.InternalColorPicker.Color.ToHex();
            }
            else
            {
                ViewModel.GetAdmConfig().ColorizationSwitch.Component.DarkHex = dialogContent.InternalColorPicker.Color.ToHex();
            }
            ViewModel.AccentColorMode = ColorizationViewModel.ThemeColorMode.Manual;

            ViewModel.SafeSaveBuilder();
            if (ViewModel.GetAdmConfig().ColorizationSwitch.Enabled)
            {
                ViewModel.RequestThemeSwitch();
            }
        }
    }
}
