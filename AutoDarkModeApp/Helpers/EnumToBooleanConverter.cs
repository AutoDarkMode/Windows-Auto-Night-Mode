using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public partial class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value?.ToString() == parameter?.ToString())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool booleanValue && booleanValue == true && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        return null;
    }
}
