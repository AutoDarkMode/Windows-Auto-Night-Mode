using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using AutoDarkModeLib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace AutoDarkModeApp.UserControls;

public sealed partial class DesktopPreview : UserControl
{
    public ImageSource? GlobalWallpaperSource
    {
        get => (ImageSource)GetValue(GlobalWallpaperSourceProperty);
        set => SetValue(GlobalWallpaperSourceProperty, value);
    }

    public static readonly DependencyProperty GlobalWallpaperSourceProperty = DependencyProperty.Register(
        "GlobalWallpaperSource",
        typeof(ImageSource),
        typeof(DesktopPreview),
        new PropertyMetadata(null)
    );

    public ElementTheme DesktopPreviewThemeMode
    {
        get => (ElementTheme)GetValue(DesktopPreviewThemeModeProperty);
        set => SetValue(DesktopPreviewThemeModeProperty, value);
    }

    public static readonly DependencyProperty DesktopPreviewThemeModeProperty = DependencyProperty.Register(
        "DesktopPreviewThemeMode",
        typeof(ElementTheme),
        typeof(DesktopPreview),
        new PropertyMetadata(ElementTheme.Default)
    );

    public DesktopPreview()
    {
        InitializeComponent();
        InitializePreview();
    }

    private void InitializePreview()
    {
        GlobalWallpaperSource = GetCurrentWallpaper();
        DesktopPreviewThemeMode = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
    }

    private static ImageSource GetCurrentWallpaper()
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SystemParametersInfo(uint uAction, uint uParam, StringBuilder lpvParam, uint init);

        const uint SPI_GETDESKWALLPAPER = 0x0073;

        var wallPaperPath = new StringBuilder(200);
        if (SystemParametersInfo(SPI_GETDESKWALLPAPER, 200, wallPaperPath, 0) && !string.IsNullOrEmpty(wallPaperPath.ToString()))
        {
            return new BitmapImage(new Uri(wallPaperPath.ToString()));
        }
        else if (string.IsNullOrEmpty(wallPaperPath.ToString()))
        {
            string? colorStr = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Colors", "Background", "")?.ToString();

            if (!string.IsNullOrEmpty(colorStr))
            {
                var parts = colorStr.Split(' ');
                var bitmap = new WriteableBitmap(1920, 1080);

                int pixelCount = 1920 * 1080;
                byte[] pixels = new byte[pixelCount * 4];

                for (int i = 0; i < pixelCount; i++)
                {
                    int index = i * 4;
                    pixels[index + 0] = Convert.ToByte(parts[2]);
                    pixels[index + 1] = Convert.ToByte(parts[1]);
                    pixels[index + 2] = Convert.ToByte(parts[0]);
                    pixels[index + 3] = 255;
                }

                using (var stream = bitmap.PixelBuffer.AsStream())
                {
                    stream.Write(pixels, 0, pixels.Length);
                }
                return bitmap;
            }
        }
        else
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_24H2)
            {
                var spotlitghtWallpaperPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\DesktopSpotlight\Assets\Images\image_1.jpg"
                );
                if (File.Exists(spotlitghtWallpaperPath))
                {
                    return new BitmapImage(new Uri(spotlitghtWallpaperPath));
                }
            }
            else
            {
                var spotlitghtWallpaperPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes\TranscodedWallpaper");
                if (File.Exists(spotlitghtWallpaperPath))
                {
                    return new BitmapImage(new Uri(spotlitghtWallpaperPath));
                }
            }
        }
        return new BitmapImage();
    }
}
