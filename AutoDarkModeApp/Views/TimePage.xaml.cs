using System.Globalization;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Views;

public sealed partial class TimePage : Page
{
    public TimeViewModel ViewModel { get; }

    public TimePage()
    {
        ViewModel = App.GetService<TimeViewModel>();
        InitializeComponent();
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
}
