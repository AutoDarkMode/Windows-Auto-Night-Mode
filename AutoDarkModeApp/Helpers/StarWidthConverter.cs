using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace AutoDarkModeApp.Helpers;

/// <summary>
/// Converts a double value to a Star GridLength.
/// Used for positioning elements proportionally in a Grid.
/// </summary>
public class StarWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double val)
        {
            return new GridLength(val, GridUnitType.Star);
        }
        return new GridLength(0, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is GridLength gl)
        {
            return gl.Value;
        }
        return 0.0;
    }
}
