using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for PageWallpaper.xaml
    /// </summary>
    public partial class PageWallpaper : Page
    {
        public PageWallpaper()
        {
            InitializeComponent();
        }
        private void ShowDeskBGStatus()
        {
            if (Properties.Settings.Default.WallpaperSwitch == true)
            {
                DeskBGStatus.Text = Properties.Resources.enabled;
            }
            else
            {
                DeskBGStatus.Text = Properties.Resources.disabled;
            }
        }
        private void BGWinButton_Click(object sender, RoutedEventArgs e)
        {
            DesktopBGui BGui = new DesktopBGui();
            BGui.ShowDialog();
            if (BGui.saved == true)
            {
                //ApplyButton_Click(this, null);
            }
            ShowDeskBGStatus();
        }
    }
}
