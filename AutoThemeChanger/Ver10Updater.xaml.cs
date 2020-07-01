using AutoThemeChanger.Properties;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for Ver10Updater.xaml
    /// </summary>
    public partial class Ver10Updater : Window
    {
        string updateURL;

        public Ver10Updater(string pURL)
        {
            InitializeComponent();
            this.updateURL = pURL;
        }

        private void ButtonNeverUpdate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MsgBox msg = new MsgBox(Properties.Resources.VersionXUpdaterNeverDescription, Properties.Resources.VersionXUpdaterNeverTitle, "info", "yesno");
            msg.Owner = GetWindow(this);
            msg.ShowDialog();
            var result = msg.DialogResult;
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
            Process.Start(updateURL);
            Close();
        }
    }
}
