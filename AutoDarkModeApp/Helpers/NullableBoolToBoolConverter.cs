using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

public partial class NullableBoolToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && b == true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
