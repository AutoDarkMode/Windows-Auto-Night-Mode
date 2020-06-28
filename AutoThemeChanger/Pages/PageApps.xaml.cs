using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for PageApps.xaml
    /// </summary>
    public partial class PageApps : Page
    {
        readonly RegeditHandler regEditHandler = new RegeditHandler();
        bool is1903 = false;

        public PageApps()
        {
            InitializeComponent();
            UiHandler();

            //follow windows theme
            ThemeChange(this, null);
            SourceChord.FluentWPF.SystemTheme.ThemeChanged += ThemeChange;
        }

        //react to windows theme change
        private void ThemeChange(object sender, EventArgs e)
        {
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark))
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"/Resources/Microsoft_Edge_Logo_White.png", UriKind.Relative));
            }
            else
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"/Resources/Microsoft_Edge_Logo.png", UriKind.Relative));
            }
        }

        private void UiHandler()
        {
            //if automatic theme switch isn't enabled
            if (!Properties.Settings.Default.Enabled)
            {
                AccentColorCheckBox.IsEnabled = false;
                SystemComboBox.IsEnabled = false;
                AppComboBox.IsEnabled = false;
                EdgeComboBox.IsEnabled = false;
                OfficeComboBox.IsEnabled = false;
                CheckBoxOfficeWhiteTheme.IsEnabled = false;
            }

            //if a windows theme file was picked
            if (Properties.Settings.Default.ThemeSwitch)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                AppComboBox.IsEnabled = false;
                AppComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
            }

            //if the OS version is older than 1903
            if (int.Parse(regEditHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            if (!is1903)
            {
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.cmb1903;
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.cmb1903;
            }
            else
            //os version 1903+
            {
                //inform user about settings
                if(!Properties.Settings.Default.ThemeSwitch) AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;

                //is accent color switch enabled?
                AccentColorCheckBox.IsChecked = Properties.Settings.Default.AccentColor;
            }

            //combobox
            AppComboBox.SelectedIndex = Properties.Settings.Default.AppThemeChange;
            SystemComboBox.SelectedIndex = Properties.Settings.Default.SystemThemeChange;
            EdgeComboBox.SelectedIndex = Properties.Settings.Default.EdgeThemeChange;
            OfficeComboBox.SelectedIndex = Properties.Settings.Default.OfficeThemeChange;

            //checkbox
            CheckBoxOfficeWhiteTheme.IsChecked = Properties.Settings.Default.OfficeThemeChangeWhiteDesign;
        }

        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (AppComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.AppThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }

            }

            if (AppComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.AppThemeChange = 1;
                regEditHandler.SetAppTheme(1);
            }

            if (AppComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.AppThemeChange = 2;
                regEditHandler.SetAppTheme(0);
            }
        }

        private void SystemComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.SystemThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
                AccentColorCheckBox.IsEnabled = true;
            }

            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.SystemThemeChange = 1;
                if (Properties.Settings.Default.AccentColor)
                {
                    regEditHandler.ColorPrevalence(0);
                    Thread.Sleep(Properties.Settings.Default.AccentColorSwitchTime);
                }
                regEditHandler.SetSystemTheme(1);
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.IsChecked = false;
            }

            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                regEditHandler.SetSystemTheme(0);
                if (Properties.Settings.Default.AccentColor)
                {
                    Thread.Sleep(Properties.Settings.Default.AccentColorSwitchTime);
                    regEditHandler.ColorPrevalence(1);
                }
                AccentColorCheckBox.IsEnabled = true;
            }
        }

        private void EdgeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (EdgeComboBox.SelectedIndex.Equals(0))
            {
                try
                {
                    Properties.Settings.Default.EdgeThemeChange = 0;
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {
                    DisableEdgeSwitch();
                }
            }

            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                try
                {
                    regEditHandler.SetEdgeTheme(0);
                    Properties.Settings.Default.EdgeThemeChange = 1;
                }
                catch
                {
                    DisableEdgeSwitch();
                }
            }

            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                try
                {
                    regEditHandler.SetEdgeTheme(1);
                    Properties.Settings.Default.EdgeThemeChange = 2;
                }
                catch
                {
                    DisableEdgeSwitch();
                }
            }

            if (EdgeComboBox.SelectedIndex.Equals(3))
            {
                Properties.Settings.Default.EdgeThemeChange = 3;
            }
        }
        private void DisableEdgeSwitch()
        {
            Properties.Settings.Default.EdgeThemeChange = 3;
            EdgeComboBox.SelectedIndex = 3;
        }

        private void AccentColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked ?? false)
            {
                try
                {
                    Properties.Settings.Default.AccentColor = true;
                    if (SystemComboBox.SelectedIndex.Equals(0)) regEditHandler.SwitchThemeBasedOnTime();
                    if (SystemComboBox.SelectedIndex.Equals(2)) regEditHandler.ColorPrevalence(1);
                }
                catch
                {
                    AccentColorCheckBox.IsChecked = false;
                    Properties.Settings.Default.AccentColor = false;
                }
            }
            else
            {
                Properties.Settings.Default.AccentColor = false;
                regEditHandler.ColorPrevalence(0);
            }
        }

        private void OfficeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (OfficeComboBox.SelectedIndex.Equals(0))
            {
                try
                {
                    Properties.Settings.Default.OfficeThemeChange = 0;
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {
                    DisableOfficeSwitch();
                }
            }

            if (OfficeComboBox.SelectedIndex.Equals(1))
            {
                try
                {
                    if (!Properties.Settings.Default.OfficeThemeChangeWhiteDesign) regEditHandler.OfficeTheme(0);
                    else regEditHandler.OfficeTheme(5);
                    Properties.Settings.Default.OfficeThemeChange = 1;
                }
                catch
                {
                    DisableOfficeSwitch();
                }
            }

            if (OfficeComboBox.SelectedIndex.Equals(2))
            {
                try
                {
                    regEditHandler.OfficeTheme(4);
                    Properties.Settings.Default.OfficeThemeChange = 2;
                }
                catch
                {
                    DisableOfficeSwitch();
                }
            }

            if (OfficeComboBox.SelectedIndex.Equals(3))
            {
                Properties.Settings.Default.OfficeThemeChange = 3;
            }
        }
        private void DisableOfficeSwitch()
        {
            Properties.Settings.Default.OfficeThemeChange = 3;
            OfficeComboBox.SelectedIndex = 3;
        }

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Every-website-in-dark-mode-with-Dark-Reader");
        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            if(CheckBoxOfficeWhiteTheme.IsChecked ?? true){
                Properties.Settings.Default.OfficeThemeChangeWhiteDesign = true;
                OfficeComboBox_DropDownClosed(this, null);
            }
            else
            {
                Properties.Settings.Default.OfficeThemeChangeWhiteDesign = false;
                OfficeComboBox_DropDownClosed(this, null);
            }
        }
    }
}