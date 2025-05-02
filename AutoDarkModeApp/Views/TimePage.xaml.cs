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

        args.Cancel = !args.NewText.All(c => char.IsDigit(c) || c == decimalSeparator[0] || c == '.' || c == '-');

        if (args.NewText.Count(i => ".".Contains(i)) > 1)
            args.Cancel = true;

        if (args.NewText.Count(i => decimalSeparator[0].ToString().Contains(i)) > 1)
            args.Cancel = true;

        if (args.NewText.Contains('.'))
        {
            string[] parts = args.NewText.Split('.');
            if (parts.Length >= 2 && parts[1].Length > 6)
            {
                args.Cancel = true;
            }
        }

        if (args.NewText.Contains(decimalSeparator))
        {
            string[] parts = args.NewText.Split(decimalSeparator);
            if (parts.Length >= 2 && parts[1].Length > 6)
            {
                args.Cancel = true;
            }
        }
    }
}
