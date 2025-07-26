using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ColorPickerDialogContentControl : UserControl
{
    public Color CustomColor
    {
        get => (Color)GetValue(CustomColorProperty);
        set => SetValue(CustomColorProperty, value);
    }

    public static readonly DependencyProperty CustomColorProperty = DependencyProperty.Register(
        "CustomColor",
        typeof(Color),
        typeof(ColorPickerDialogContentControl),
        new PropertyMetadata(Colors.White)
    );

    public ColorPickerDialogContentControl()
    {
        InitializeComponent();
    }

    private void SelectColorColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        CustomColor = args.NewColor;
    }
}
