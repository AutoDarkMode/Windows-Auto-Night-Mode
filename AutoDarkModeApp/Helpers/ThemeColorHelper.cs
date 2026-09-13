using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AutoDarkModeApp.Helpers;

internal static class ThemeColorHelper
{
    public static Color? GetThemeColor(string resourceKey)
    {
        try
        {
            object? resource = null;

            if (Application.Current.Resources.TryGetValue(resourceKey, out var appValue))
            {
                resource = appValue;
            }

            if (resource is SolidColorBrush solidBrush)
            {
                return solidBrush.Color;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetThemeColor failed for '{resourceKey}': {ex.Message}");
        }

        return null;
    }
}
