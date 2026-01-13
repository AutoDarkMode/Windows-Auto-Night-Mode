using Microsoft.UI.Xaml.Data;

namespace AutoDarkModeApp.Helpers;

/// <summary>
/// Converts between actual lux values (0-10000) and a logarithmic slider position (0-1000).
/// This gives fine control at low lux values while supporting high brightness levels.
/// Using 1000 steps provides enough precision to hit most integer lux values.
/// </summary>
public partial class LogarithmicLuxConverter : IValueConverter
{
    // Maximum lux value supported
    private const double MaxLux = 10000.0;
    // Slider range (0-1000 for finer precision)
    private const double SliderMax = 1000.0;
    // Precomputed log constant for performance
    private static readonly double LogBase = Math.Log(MaxLux + 1);

    /// <summary>
    /// Convert lux value to slider position (logarithmic scale)
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double lux)
        {
            // Handle edge case
            if (lux <= 0) return 0.0;
            if (lux >= MaxLux) return SliderMax;

            // Logarithmic conversion: slider = ln(lux + 1) / ln(maxLux + 1) * sliderMax
            return Math.Log(lux + 1) / LogBase * SliderMax;
        }
        return 0.0;
    }

    /// <summary>
    /// Convert slider position to lux value (exponential scale)
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double sliderValue)
        {
            // Handle edge cases
            if (sliderValue <= 0) return 0.0;
            if (sliderValue >= SliderMax) return MaxLux;

            // Exponential conversion: lux = e^(slider / sliderMax * ln(maxLux + 1)) - 1
            double lux = Math.Exp(sliderValue / SliderMax * LogBase) - 1;

            // Round to reasonable precision (whole numbers for most values)
            if (lux < 100) return Math.Round(lux);
            if (lux < 1000) return Math.Round(lux / 5) * 5;
            return Math.Round(lux / 10) * 10;
        }
        return 0.0;
    }
}
