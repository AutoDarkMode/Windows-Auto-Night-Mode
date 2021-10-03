
using System.Windows;
using System.Windows.Controls;
using ModernWpf.Media.Animation;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PagePersonalization.xaml
    /// </summary>
    public partial class PagePersonalization : ModernWpf.Controls.Page
    {
        public PagePersonalization()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PageThemePicker), null, new DrillInNavigationTransitionInfo());
        }

        private void TestButton2_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PageWallpaperPicker), null, new DrillInNavigationTransitionInfo());
        }
    }
}
