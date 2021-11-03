using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageDonation.xaml
    /// </summary>
    public partial class PageDonation : Page
    {
        public PageDonation()
        {
            InitializeComponent();
        }

        private void ButtonPayPal_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://paypal.me/arminosaj");
        }

        private static void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
