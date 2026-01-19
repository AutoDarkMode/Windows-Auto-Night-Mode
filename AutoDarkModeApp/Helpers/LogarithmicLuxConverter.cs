using System;
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
    public const double MaxLuxValue = 10000.0;
    // Slider range (0-1000 for finer precision)
    public const double SliderMaxValue = 1000.0;
    // Log10(Max) = 4.0
    private const double LogMax = 4.0;

    /// <summary>
    /// Static conversion from lux to slider position
    /// </summary>
    public static double LuxToSlider(double lux)
    {
        if (lux <= 1) return 0.0;
        if (lux >= MaxLuxValue) return SliderMaxValue;

        // Log10 conversion: slider = log10(lux) / 4 * 1000
        // Since log10(1) = 0, this maps 1->0
        return Math.Log10(lux) / LogMax * SliderMaxValue;
    }

    /// <summary>
    /// Static conversion from slider position to lux
    /// </summary>
    public static double SliderToLux(double sliderValue)
    {
        if (sliderValue <= 0) return 1.0; // Minimum is 1
        if (sliderValue >= SliderMaxValue) return MaxLuxValue;

        // Exponential conversion: lux = 10 ^ (slider / 1000 * 4)
        double lux = Math.Pow(10, sliderValue / SliderMaxValue * LogMax);

        // Round to reasonable precision (whole numbers for most values)
        if (lux < 100) return Math.Round(lux);
        if (lux < 1000) return Math.Round(lux / 5) * 5;
        return Math.Round(lux / 10) * 10;
    }

    /// <summary>
    /// Convert lux value to slider position (logarithmic scale)
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double lux)
        {
            return LuxToSlider(lux);
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
            return SliderToLux(sliderValue);
        }
        return 0.0;
    }
}
