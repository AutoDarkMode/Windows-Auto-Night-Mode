using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ColorPickerDialogContentControl : UserControl
{
    public ColorPicker InternalColorPicker => DialogContentColorPicker;

    public ColorPickerDialogContentControl()
    {
        InitializeComponent();
    }
}
