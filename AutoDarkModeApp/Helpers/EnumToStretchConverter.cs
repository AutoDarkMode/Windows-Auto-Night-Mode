using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.Helpers;

public partial class EnumToStretchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var mode = (WallpaperFillingMode)value;
        return mode switch
        {
            WallpaperFillingMode.Center => Stretch.Uniform,
            WallpaperFillingMode.Stretch => Stretch.Fill,
            WallpaperFillingMode.Fit => Stretch.Uniform,
            WallpaperFillingMode.Fill => Stretch.UniformToFill,
            // shortcut: Tile previews one image, replace when preview tiling is implemented.
            WallpaperFillingMode.Tile => Stretch.Uniform,
            _ => Stretch.None,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("Two-way binding is not supported for EnumToStretchConverter.");
    }
}

public enum WallpaperFillingMode
{
    Center,
    Stretch,
    Fit,
    Fill,
    Tile,
}
