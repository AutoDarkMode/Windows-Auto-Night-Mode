using System.Diagnostics;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace AutoDarkModeApp.Views;

public sealed partial class DonationPage : Page
{
    private readonly Random _random = new();

    public DonationViewModel ViewModel { get; }

    public DonationPage()
    {
        ViewModel = App.GetService<DonationViewModel>();
        InitializeComponent();
    }

    private void DonationPictureBorder_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        int heartCount = _random.Next(10, 16);

        for (int i = 0; i < heartCount; i++)
        {
            CreateFloatingHeart(e.GetCurrentPoint(this));
        }
    }

    private void CreateFloatingHeart(PointerPoint pointerPoint)
    {
        var colors = new Windows.UI.Color[]
        {
            Windows.UI.Color.FromArgb(255, 0xF2, 0xC9, 0x4C), // #f2c94c
            Windows.UI.Color.FromArgb(255, 0xF0, 0x7B, 0x2E), // #f07b2e
            Windows.UI.Color.FromArgb(255, 0xD7, 0x4B, 0x24), // #d74b24
            Windows.UI.Color.FromArgb(255, 0x7B, 0x3E, 0x9D), // #7b3e9d
            Windows.UI.Color.FromArgb(255, 0x4A, 0x9F, 0xE1), // #4a9fe1
            Windows.UI.Color.FromArgb(255, 0x68, 0xA9, 0xA0), // #68a9a0
            Windows.UI.Color.FromArgb(255, 0x58, 0xC9, 0x9A), // #58c99a
            Windows.UI.Color.FromArgb(255, 0xF6, 0x9A, 0x7E), // #f69a7e
            Windows.UI.Color.FromArgb(255, 0xC1, 0x2c, 0x3E), // #c12c3e
        };
        var randomColor = colors[_random.Next(colors.Length)];

        var heart = new FontIcon
        {
            Glyph = "\uEB52",
            FontSize = _random.Next(20, 40),
            Opacity = 1.0,
            Foreground = new SolidColorBrush(randomColor),
        };

        var mousePosition = pointerPoint.Position;
        double startX = (mousePosition.X) + _random.Next(-100, 100);
        double startY = (mousePosition.Y) + _random.Next(-50, 50);

        Canvas.SetLeft(heart, startX);
        Canvas.SetTop(heart, startY);

        EffectCanvas.Children.Add(heart);

        CreateHeartAnimation(heart, startX, startY);
    }

    private void CreateHeartAnimation(FontIcon heart, double startX, double startY)
    {
        Duration duration = new Duration(TimeSpan.FromSeconds(2 + _random.NextDouble()));

        DoubleAnimation moveY = new DoubleAnimation
        {
            From = startY,
            To = startY - _random.Next(200, 400),
            Duration = duration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };

        DoubleAnimation moveX = new DoubleAnimation
        {
            From = startX,
            To = startX + _random.Next(-100, 100),
            Duration = duration,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };

        DoubleAnimation fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = duration,
            BeginTime = TimeSpan.FromSeconds(duration.TimeSpan.TotalSeconds * 0.5),
        };

        Storyboard storyboard = new Storyboard();

        Storyboard.SetTarget(moveY, heart);
        Storyboard.SetTargetProperty(moveY, "(Canvas.Top)");

        Storyboard.SetTarget(moveX, heart);
        Storyboard.SetTargetProperty(moveX, "(Canvas.Left)");

        Storyboard.SetTarget(fadeOut, heart);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");

        storyboard.Children.Add(moveY);
        storyboard.Children.Add(moveX);
        storyboard.Children.Add(fadeOut);

        storyboard.Completed += (s, e) =>
        {
            EffectCanvas.Children.Remove(heart);
        };

        storyboard.Begin();
    }

    private void DonationPayPalButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartProcessByProcessInfo("https://www.paypal.com/donate/?hosted_button_id=H65KZYMHKCB6E");
    }

    private void GithubSponsorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StartProcessByProcessInfo("https://github.com/sponsors/AutoDarkMode");
    }

    private static void StartProcessByProcessInfo(string message)
    {
        Process.Start(new ProcessStartInfo(message) { UseShellExecute = true, Verb = "open" });
    }
}
