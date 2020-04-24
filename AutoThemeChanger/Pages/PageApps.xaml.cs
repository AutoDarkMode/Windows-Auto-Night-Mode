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
        RegeditHandler regEditHandler = new RegeditHandler();
        bool is1903 = false;

        public PageApps()
        {
            InitializeComponent();
            UiHandler();
            
            //follow windows theme
            ThemeChange(this, null);
            SourceChord.FluentWPF.SystemTheme.ThemeChanged += ThemeChange;
        }

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
            }

            //if a windows theme was picked
            if (Properties.Settings.Default.ThemeSwitch)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme; ;
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
            {
                AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;
            }

            //accent color switch
            if (Properties.Settings.Default.AccentColor)
            {
                AccentColorCheckBox.IsChecked = true;
            }

            //combobox
            int appTheme = Properties.Settings.Default.AppThemeChange;
            if (appTheme == 0) AppComboBox.SelectedIndex = 0;
            if (appTheme == 1) AppComboBox.SelectedIndex = 1;
            if (appTheme == 2) AppComboBox.SelectedIndex = 2;

            int systemTheme = Properties.Settings.Default.SystemThemeChange;
            if (systemTheme == 0) SystemComboBox.SelectedIndex = 0;
            if (systemTheme == 1) SystemComboBox.SelectedIndex = 1;
            if (systemTheme == 2) SystemComboBox.SelectedIndex = 2;

            int edgeTheme = Properties.Settings.Default.EdgeThemeChange;
            if (edgeTheme == 0) EdgeComboBox.SelectedIndex = 0;
            if (edgeTheme == 1) EdgeComboBox.SelectedIndex = 1;
            if (edgeTheme == 2) EdgeComboBox.SelectedIndex = 2;
            if (edgeTheme == 3) EdgeComboBox.SelectedIndex = 3;
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
                regEditHandler.AppTheme(1);
            }
            if (AppComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.AppThemeChange = 2;
                regEditHandler.AppTheme(0);
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
                regEditHandler.SystemTheme(1);
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.IsChecked = false;
            }
            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                regEditHandler.SystemTheme(0);
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
                Properties.Settings.Default.EdgeThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
            }
            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.EdgeThemeChange = 1;
                regEditHandler.EdgeTheme(0);
            }
            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.EdgeThemeChange = 2;
                regEditHandler.EdgeTheme(1);
            }
            if (EdgeComboBox.SelectedIndex.Equals(3))
            {
                Properties.Settings.Default.EdgeThemeChange = 3;
            }
        }
        private void AccentColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AccentColor = true;
            try
            {
                if (SystemComboBox.SelectedIndex.Equals(0)) regEditHandler.SwitchThemeBasedOnTime();
                if (SystemComboBox.SelectedIndex.Equals(2)) regEditHandler.ColorPrevalence(1);
            }
            catch
            {

            }
        }
        private void AccentColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AccentColor = false;
            regEditHandler.ColorPrevalence(0);
        }
    }
}
