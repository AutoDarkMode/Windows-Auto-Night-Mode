using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public class EnumToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return 0;
        return (int)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int index)
        {
            if (Enum.IsDefined(targetType, index))
            {
                return Enum.ToObject(targetType, index);
            }
        }
        return 0;
    }
}