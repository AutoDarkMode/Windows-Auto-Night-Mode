using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace AutoDarkModeApp.Helpers;

public partial class EnumToStretchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is WallpaperFillingMode mode)
        {
            return mode switch
            {
                WallpaperFillingMode.Fill => Stretch.UniformToFill,
                WallpaperFillingMode.Fit => Stretch.Uniform,
                WallpaperFillingMode.Stretch => Stretch.Fill,
                _ => Stretch.None,
            };
        }
        return Stretch.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("Two-way binding is not supported for EnumToStretchConverter.");
    }
}

public enum WallpaperFillingMode
{
    Fill,
    Fit,
    Stretch
}
