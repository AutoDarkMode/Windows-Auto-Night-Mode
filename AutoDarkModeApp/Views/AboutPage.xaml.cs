using System.Diagnostics;
using System.Text;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Utils;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Xaml.Controls;

using AdmExtensions = AutoDarkModeLib.Helper;

namespace AutoDarkModeApp.Views;

public sealed partial class AboutPage : Page
{
    private readonly IErrorService errorService = App.GetService<IErrorService>();

    public AboutViewModel ViewModel { get; }

    public AboutPage()
    {
        ViewModel = App.GetService<AboutViewModel>();
        InitializeComponent();
    }

    private void CopyVersionInfoButoon_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // most likely use case is to paste in an issue, so
        // we create a markdown string that will look nice
        // in that context
        var versionInfo = new VersionInfo();
        var versionText = new StringBuilder()
                .Append("- Commit: `")
                .Append(versionInfo.Commit)
                .AppendLine("`")
                .Append("- Service/App: `")
                .Append(versionInfo.Svc)
                .AppendLine("`")
                .Append("- Updater: `")
                .Append(versionInfo.Updater)
                .AppendLine("`")
                .Append("- Shell: `")
                .Append(versionInfo.Shell)
                .AppendLine("`")
                .Append("- .Net: `")
                .Append(versionInfo.NetCore)
                .AppendLine("`")
                .Append("- Windows: `")
                .Append(versionInfo.WindowsVersion)
                .AppendLine("`")
                .Append("- Arch: `")
                .Append(versionInfo.Arch)
                .AppendLine("`")
                .ToString();
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(versionText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            CopyButtonTeachingTip.IsOpen = true;
        }
        catch (Exception ex)
        {
            errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AboutPage_CopyVersionInfoButton");
        }
    }

    private void LogoImage_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
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

        var random = new Random();

        var randomColor = colors[random.Next(colors.Length)];

        AppNameTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(randomColor);
    }

    private void OpenLogHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "service.log");
        try
        {
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }
        catch (Exception ex)
        {
            errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AboutPage");
        }
    }

    private void OpenUpdaterLogHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "updater.log");
        try
        {
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }
        catch (Exception ex)
        {
            errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AboutPage");
        }
    }

    private void OpenShellHyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var filepath = AdmExtensions.ExectuionPathShell;
        try
        {
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }
        catch (Exception ex)
        {
            errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "AboutPage");
        }
    }
}
