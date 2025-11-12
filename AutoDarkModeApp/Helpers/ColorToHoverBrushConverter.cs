using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoDarkModeApp.Helpers;

public partial class ColorToHoverBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Color color)
        {
            var hoverColor = GetHoverColor(color);
            return new SolidColorBrush(hoverColor);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static Color GetHoverColor(Color baseColor)
    {
        const double factor = 0.4;
        return Color.FromArgb(baseColor.A, (byte)Math.Min(255, baseColor.R * factor), (byte)Math.Min(255, baseColor.G * factor), (byte)Math.Min(255, baseColor.B * factor));
    }
}
