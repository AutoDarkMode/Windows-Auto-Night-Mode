using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.UserControls;
using AutoDarkModeApp.ViewModels;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace AutoDarkModeApp.Views;

public sealed partial class ColorizationPage : Page
{
    public ColorizationViewModel ViewModel { get; }

    public ColorizationPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<ColorizationViewModel>();

        PopulateColorGrid();
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

    private void PopulateColorGrid()
    {
        var colors = new[]
        {
            Color.FromArgb(255, 255, 185, 0), /* #FFB900 */
            Color.FromArgb(255, 255, 140, 0), /* #FF8C00 */
            Color.FromArgb(255, 247, 99, 13), /* #F7630D */
            Color.FromArgb(255, 202, 80, 16), /* #CA5010 */
            Color.FromArgb(255, 218, 59, 1), /* #DA3B01 */
            Color.FromArgb(255, 239, 105, 80), /* #EF6950 */
            Color.FromArgb(255, 209, 52, 56), /* #D13438 */
            Color.FromArgb(255, 255, 67, 67), /* #FF4343 */
            Color.FromArgb(255, 231, 72, 86), /* #E74856 */
            Color.FromArgb(255, 232, 17, 35), /* #E81123 */
            Color.FromArgb(255, 234, 0, 94), /* #EA005E */
            Color.FromArgb(255, 195, 0, 82), /* #C30052 */
            Color.FromArgb(255, 227, 0, 140), /* #E3008C */
            Color.FromArgb(255, 191, 0, 119), /* #BF0077 */
            Color.FromArgb(255, 194, 57, 179), /* #C239B3 */
            Color.FromArgb(255, 154, 0, 137), /* #9A0089 */
            Color.FromArgb(255, 0, 120, 212), /* #0078D4 */
            Color.FromArgb(255, 0, 99, 177), /* #0063B1 */
            Color.FromArgb(255, 142, 140, 216), /* #8E8CD8 */
            Color.FromArgb(255, 107, 105, 214), /* #6B69D6 */
            Color.FromArgb(255, 135, 100, 184), /* #8764B8 */
            Color.FromArgb(255, 116, 77, 169), /* #744DA9 */
            Color.FromArgb(255, 177, 70, 194), /* #B146C2 */
            Color.FromArgb(255, 136, 23, 152), /* #881798 */
            Color.FromArgb(255, 0, 153, 188), /* #0099BC */
            Color.FromArgb(255, 45, 125, 154), /* #2D7D9A */
            Color.FromArgb(255, 0, 183, 195), /* #00B7C3 */
            Color.FromArgb(255, 3, 131, 135), /* #038387 */
            Color.FromArgb(255, 0, 178, 148), /* #00B294 */
            Color.FromArgb(255, 1, 133, 116), /* #018574 */
            Color.FromArgb(255, 0, 204, 106), /* #00CC6A */
            Color.FromArgb(255, 16, 137, 62), /* #10893E */
            Color.FromArgb(255, 122, 117, 116), /* #7A7574 */
            Color.FromArgb(255, 93, 90, 88), /* #5D5A58 */
            Color.FromArgb(255, 104, 118, 138), /* #68768A */
            Color.FromArgb(255, 81, 92, 107), /* #515C6B */
            Color.FromArgb(255, 86, 124, 115), /* #567C73 */
            Color.FromArgb(255, 72, 104, 96), /* #486860 */
            Color.FromArgb(255, 73, 130, 5), /* #498205 */
            Color.FromArgb(255, 118, 118, 118), /* #767676 */
            Color.FromArgb(255, 76, 74, 72), /* #4C4A48 */
            Color.FromArgb(255, 76, 74, 72), /* #4C4A48 */
            Color.FromArgb(255, 105, 121, 126), /* #69797E */
            Color.FromArgb(255, 74, 84, 89), /* #4A5459 */
            Color.FromArgb(255, 100, 124, 100), /* #647C64 */
            Color.FromArgb(255, 82, 94, 84), /* #525E54 */
            Color.FromArgb(255, 132, 117, 69), /* #847545 */
            Color.FromArgb(255, 126, 115, 95), /* #7E735F */
        };

        foreach (var color in colors)
        {
            var displayName = Microsoft.UI.ColorHelper.ToDisplayName(color);

            var rectangle = new Rectangle { Fill = new SolidColorBrush(color) };

            AutomationProperties.SetName(rectangle, displayName);
            ToolTipService.SetToolTip(rectangle, displayName);

            WindowsColorGridView.Items.Add(rectangle);
        }
    }
}
