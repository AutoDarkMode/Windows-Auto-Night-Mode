using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility && (Visibility)value == Visibility.Visible;
    }
}
