using System.Runtime.InteropServices;
using System.Text;
using AutoDarkModeLib;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Windows.UI;

namespace AutoDarkModeApp.UserControls;

public sealed partial class DesktopPreview : UserControl
{
    [GeneratedDependencyProperty]
    public partial SolidColorBrush? DesktopPreviewBackground { get; set; }

    [GeneratedDependencyProperty]
    public partial ImageSource? DesktopPreviewImageSource { get; set; }

    [GeneratedDependencyProperty]
    public partial ElementTheme DesktopPreviewThemeMode { get; set; }

    [GeneratedDependencyProperty]
    public partial bool DesktopPreviewAutomaticPreviewEnable { get; set; }

    [GeneratedDependencyProperty]
    public partial bool DesktopPreviewAccentColorBorderVisible { get; set; }

    [GeneratedDependencyProperty]
    public partial SolidColorBrush? DesktopPreviewAccentColorBorderBackground { get; set; }

    public DesktopPreview()
    {
        InitializeComponent();
    }

    private void InitializePreview()
    {
        DesktopPreviewImageSource = GetCurrentWallpaper();
        DesktopPreviewThemeMode = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
    }

    private BitmapImage? GetCurrentWallpaper()
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
                DesktopPreviewBackground = new SolidColorBrush(Color.FromArgb(255, Convert.ToByte(parts[0]), Convert.ToByte(parts[1]), Convert.ToByte(parts[2])));
                return null;
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

    partial void OnDesktopPreviewAutomaticPreviewEnableChanged(bool newValue)
    {
        if (newValue)
        {
            InitializePreview();
        }
    }
}
