using System.Diagnostics;
using System.Globalization;
using AutoDarkModeApp.ViewModels;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace AutoDarkModeApp.Views;

public sealed partial class TimePage : Page
{
    public TimeViewModel ViewModel { get; }

    private static readonly double[] LuxTicks = { 1, 10, 30, 100, 300, 1000 };

    public TimePage()
    {
        ViewModel = App.GetService<TimeViewModel>();
        InitializeComponent();
        Loaded += TimePage_Loaded;
    }

    private void TimePage_Loaded(object sender, RoutedEventArgs e)
    {
        DrawTicks();
        DrawLabels();

        TickCanvas.SizeChanged += (_, _) => DrawTicks();
        LabelCanvas.SizeChanged += (_, _) => DrawLabels();

        LuxRange.PointerMoved += LuxRange_PointerMoved; // use "ValueChanging" in Toolkit v9.x
    }

    private static double LuxToRelative(double lux, double minLux, double maxLux)
    {
        return Math.Log10(lux / minLux) / Math.Log10(maxLux / minLux);
    }

    private void DrawTicks()
    {
        TickCanvas.Children.Clear();

        double width = TickCanvas.ActualWidth;
        double minLux = 1;
        double maxLux = 1000;

        foreach (var lux in LuxTicks)
        {
            double relative = LuxToRelative(lux, minLux, maxLux);
            double x = relative * width;

            var line = new Rectangle
            {
                Width = 1,
                Height = 8,
                Fill = new SolidColorBrush(Colors.Gray)
            };

            Canvas.SetLeft(line, x);
            Canvas.SetTop(line, 0);

            TickCanvas.Children.Add(line);
        }
    }

    private void DrawLabels()
    {
        LabelCanvas.Children.Clear();

        double width = LabelCanvas.ActualWidth;
        double minLux = 1;
        double maxLux = 1000;

        foreach (var lux in LuxTicks)
        {
            double relative = LuxToRelative(lux, minLux, maxLux);
            double x = relative * width;

            var label = new TextBlock
            {
                Text = lux.ToString(),
                Foreground = new SolidColorBrush(Colors.Gray)
            };

            Canvas.SetLeft(label, x - 4);
            Canvas.SetTop(label, 0);

            LabelCanvas.Children.Add(label);
        }
    }

    private void LuxRange_PointerMoved(object sender, PointerRoutedEventArgs e) // use "ValueChanging" in Toolkit v9.x
    {
        //if (!LuxRange.IsThumbDragging) return;

        var range = (RangeSelector)sender;
        ViewModel.AmbientLightDarkThreshold = ViewModel.SliderToLux(range.RangeStart);
        ViewModel.AmbientLightLightThreshold = ViewModel.SliderToLux(range.RangeEnd);

        Debug.WriteLine($"Dark below: {ViewModel.AmbientLightDarkThreshold} lux");
        Debug.WriteLine($"Light above: {ViewModel.AmbientLightLightThreshold} lux");
    }

    private void CoordinatesTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        char decimalSeparatorChar = decimalSeparator[0];

        // Check for invalid characters
        bool hasInvalidChars = !args.NewText.All(c => char.IsDigit(c) || c == decimalSeparatorChar || c == '.' || c == '-');

        // Check for multiple special characters
        bool hasMultipleDots = args.NewText.Count(c => c == '.') > 1;
        bool hasMultipleSeparators = args.NewText.Count(c => c == decimalSeparatorChar) > 1;
        bool hasMultipleMinuses = args.NewText.Count(c => c == '-') > 1;

        // Check for valid minus sign position (only first character)
        bool hasInvalidMinus = args.NewText.Contains('-') && !args.NewText.StartsWith('-');

        // Check for valid decimal separator position (not first character)
        bool hasInvalidSeparatorStart = (args.NewText.StartsWith(decimalSeparatorChar) || args.NewText.StartsWith('.')) && args.NewText.Length > 1;

        // Check decimal precision
        bool exceedsPrecision = false;
        string[] dotParts = args.NewText.Split('.');
        string[] separatorParts = args.NewText.Split(decimalSeparatorChar);

        if (dotParts.Length >= 2 && dotParts[1].Length > 6)
            exceedsPrecision = true;

        if (separatorParts.Length >= 2 && separatorParts[1].Length > 6)
            exceedsPrecision = true;

        // Combine all checks
        args.Cancel = hasInvalidChars || hasMultipleDots || hasMultipleSeparators || hasMultipleMinuses || hasInvalidMinus || hasInvalidSeparatorStart || exceedsPrecision;
    }

    private async void WindowsNightLightHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:nightlight"));
    }

    private void AmbientLightRangeSelector_Loaded(object sender, RoutedEventArgs e)
    {
        // Hide the tooltip elements in the RangeSelector control
        if (sender is FrameworkElement element)
        {
            HideTooltipElements(element);
        }
    }

    /// <summary>
    /// Recursively searches the visual tree to find and hide tooltip elements.
    /// The RangeSelector control has built-in tooltips that show raw slider values (0-1000),
    /// which don't correspond to actual lux values due to our logarithmic conversion.
    /// We hide these by setting Opacity=0 (not Visibility=Collapsed) because Collapsed
    /// leaves a thin black bar artifact from the tooltip's border/background.
    /// </summary>
    private static void HideTooltipElements(DependencyObject parent)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe && fe.Name == "ToolTip")
            {
                fe.Opacity = 0;
                fe.IsHitTestVisible = false;
            }

            HideTooltipElements(child);
        }
    }
}
