using AutoDarkModeApp.Properties;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for Ver10Updater.xaml
    /// </summary>
    public partial class Ver10Updater : Window
    {
        readonly string updateURL;

        public Ver10Updater(string pURL)
        {
            InitializeComponent();
            this.updateURL = pURL;
        }

        private void ButtonNeverUpdate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MsgBox msg = new(Properties.Resources.VersionXUpdaterNeverDescription, Properties.Resources.VersionXUpdaterNeverTitle, "info", "yesno")
            {
                Owner = GetWindow(this)
            };
            _ = msg.ShowDialog();
            bool? result = msg.DialogResult;
            if (result == true)
            {
                Settings.Default.WantsVersion10 = false;
                Close();
            }
        }

        private void ButtonJustClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonUpdateNow_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(updateURL)
            {
                UseShellExecute = true,
                Verb = "open"
            });
            Close();
        }
    }
}
