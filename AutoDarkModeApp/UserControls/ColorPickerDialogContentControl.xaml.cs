using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace AutoDarkModeApp.UserControls;

public sealed partial class ColorPickerDialogContentControl : UserControl
{
    public ColorPicker InternalColorPicker => DialogContentColorPicker;

    [GeneratedDependencyProperty]
    public partial Color ColorPickerColor { get; set; }

    public ColorPickerDialogContentControl()
    {
        InitializeComponent();
    }
}
