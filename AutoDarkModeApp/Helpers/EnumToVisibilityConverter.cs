using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value?.ToString() == parameter?.ToString())
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibilityValue && visibilityValue == Visibility.Visible && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        return null;
    }
}
