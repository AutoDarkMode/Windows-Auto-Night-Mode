using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public class FlagsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        try
        {
            var inputValue = (Enum)value;
            var targetFlag = parameter is string strParam
                ? (Enum)Enum.Parse(inputValue.GetType(), strParam)
                : (Enum)Enum.ToObject(inputValue.GetType(), parameter);

            return inputValue.HasFlag(targetFlag)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}