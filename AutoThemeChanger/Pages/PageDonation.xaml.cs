using System.Windows;
using System.Windows.Controls;

namespace AutoThemeChanger.Pages
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
            System.Diagnostics.Process.Start("https://paypal.me/arminosaj");
        }
    }
}
