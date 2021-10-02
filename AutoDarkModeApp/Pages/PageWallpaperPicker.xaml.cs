using ModernWpf.Media.Animation;
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

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageWallpaperPicker.xaml
    /// </summary>
    public partial class PageWallpaperPicker : ModernWpf.Controls.Page
    {
        public PageWallpaperPicker()
        {
            InitializeComponent();
        }

        private void ButtonNavigateBack_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PageWallpaper), null, new DrillInNavigationTransitionInfo());
        }
    }
}
