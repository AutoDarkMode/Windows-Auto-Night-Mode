using AutoDarkModeApp.Controls;
using AutoDarkModeLib;
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
    /// Interaction logic for PageColorization.xaml
    /// </summary>
    public partial class PageColorization : ModernWpf.Controls.Page
    {
        public ColorControlPanel ColorControlsLight { get { return lightColorizationSetBox; } }
        public ColorControlPanel ColorControlsDark { get { return darkColorizationSetBox; } }
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        public PageColorization()
        {
            InitializeComponent();

            Color lightColorizationColor = (Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.LightHex);
            Color darkColorizationColor = (Color)ColorConverter.ConvertFromString(builder.Config.ColorizationSwitch.Component.DarkHex);
            lightColorizationColor.A = 255;
            darkColorizationColor.A = 255;

            ColorControlsLight.InitialColorBrush = new(lightColorizationColor);
            ColorControlsLight.SelectedColorBrush = new(lightColorizationColor);
            ColorControlsDark.InitialColorBrush = new(darkColorizationColor);
            ColorControlsDark.SelectedColorBrush = new(darkColorizationColor);

            if (builder.Config.ColorizationSwitch.Enabled)
            {
                ToggleSwitchAutoColorization.IsOn = true;
            }
            else
            {
                ToggleSwitchAutoColorization.IsOn = true;
            }
        }

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PagePersonalization), null, new DrillInNavigationTransitionInfo());
        }

        private void ToggleSwitchAutoColorization_Toggled(object sender, RoutedEventArgs e)
        {

        }
    }
}
