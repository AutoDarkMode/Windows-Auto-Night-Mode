using System.Globalization;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoDarkModeApp.Views;

public sealed partial class TimePage : Page
{
    public TimeViewModel ViewModel { get; }

    public TimePage()
    {
        ViewModel = App.GetService<TimeViewModel>();
        InitializeComponent();
    }

    private async void NoLocationAccessInfoBar_ActionButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
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
        // Hide the tooltip elements in the RangeSelector control.
        if (sender is FrameworkElement element)
        {
            // Initial attempt
            HideTooltipElements(element);

            // Also schedule delayed attempts to catch lazy-loaded templates
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                HideTooltipElements(element);
            });
        }

        // Also attach to PointerEntered to ensure we catch any dynamic tooltip generation
        if (sender is UIElement uiElement)
        {
            uiElement.PointerEntered += (s, args) =>
            {
                if (s is FrameworkElement fe) HideTooltipElements(fe);
            };
        }
    }

    /// <summary>
    /// Recursively searches the visual tree to find and hide tooltip elements.
    /// The RangeSelector control has built-in tooltips that show raw slider values (0-1000).
    /// </summary>
    private static void HideTooltipElements(DependencyObject parent)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe)
            {
                // Method 1: Check name for "ToolTip"
                string name = fe.Name ?? string.Empty;
                bool isToolTip = name.Contains("ToolTip", StringComparison.OrdinalIgnoreCase);

                // Method 2: Check if it's the specific thumb parts (MinThumb/MaxThumb) and disable their ToolTipService
                if (name.Contains("Thumb", StringComparison.OrdinalIgnoreCase))
                {
                    ToolTipService.SetToolTip(fe, null);
                }

                if (isToolTip)
                {
                    fe.Opacity = 0;
                    fe.IsHitTestVisible = false;
                    fe.Visibility = Visibility.Collapsed; // Try Collapsed too
                }
            }

            HideTooltipElements(child);
        }
    }
}
