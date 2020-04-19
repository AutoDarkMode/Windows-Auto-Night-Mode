using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for PageWallpaper.xaml
    /// </summary>
    public partial class PageWallpaper : Page
    {
        readonly RegeditHandler regEditHandler = new RegeditHandler();
        bool theme1 = false;
        bool theme2 = false;

        public PageWallpaper()
        {
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            if (!Properties.Settings.Default.Enabled)
            {
                BGWinButton.IsEnabled = false;
                ComboBoxLightTheme.IsEnabled = false;
                ComboBoxDarkTheme.IsEnabled = false;
                buttonSaveTheme.IsEnabled = false;
                ButtonDisableTheme.IsEnabled = false;
            }

            if (!Properties.Settings.Default.ThemeSwitch)
            {
                ShowDeskBGStatus();
                cbSelection.SelectedIndex = 0;
            }
            else
            {
                cbSelection.SelectedIndex = 1;
                if (Properties.Settings.Default.Enabled && Properties.Settings.Default.ThemeSwitch && Properties.Settings.Default.ThemeDark != null && Properties.Settings.Default.ThemeLight != null)
                {
                    var themeNames = GetThemeFiles();
                    ComboBoxDarkTheme.ItemsSource = themeNames;
                    ComboBoxLightTheme.ItemsSource = themeNames;
                    ComboBoxLightTheme.SelectedItem = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ThemeLight);
                    ComboBoxDarkTheme.SelectedItem = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ThemeDark);
                    theme1 = true;
                    theme2 = true;
                    buttonSaveTheme.IsEnabled = true;
                }
            }
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
                ButtonDisableTheme_Click(this, e);
                regEditHandler.SwitchThemeBasedOnTime();
            }
            ShowDeskBGStatus();
        }

        private void CbSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSelection.SelectedIndex == 0)
            {
                SetWallpaperVisibility(Visibility.Visible);
                SetThemeVisibility(Visibility.Collapsed);
            }
            else if (cbSelection.SelectedIndex == 1)
            {
                SetWallpaperVisibility(Visibility.Collapsed);
                SetThemeVisibility(Visibility.Visible);
            }
        }

        private void SetWallpaperVisibility(Visibility value)
        {
            wallpaperHeading.Visibility = value;
            wallpaperIcon.Visibility = value;
            wallpaperCurrent.Visibility = value;
            DeskBGStatus.Visibility = value;
            BGWinButton.Visibility = value;
        }
        private void SetThemeVisibility(Visibility value)
        {
            ComboBoxDarkTheme.Visibility = value;
            ComboBoxLightTheme.Visibility = value;
            buttonSaveTheme.Visibility = value;
            ButtonOpenThemePath.Visibility = value;
            ButtonDisableTheme.Visibility = value;
            TbOpenThemeCP.Visibility = value;
            TbTheme1.Visibility = value;
            TbTheme2.Visibility = value;
            TbTheme3.Visibility = value;
            TbTheme4.Visibility = value;
            TbTheme5.Visibility = value;
            TbTheme6.Visibility = value;
            TbTheme7.Visibility = value;
            IconDark.Visibility = value;
            IconLight.Visibility = value;
        }

        private void EnableSaveButton()
        {
            if(theme1 == true && theme2 == true)
            {
                buttonSaveTheme.IsEnabled = true;
            }
        }

        private void ButtonSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WallpaperSwitch = false;
            Properties.Settings.Default.WallpaperDark = "";
            Properties.Settings.Default.WallpaperLight = "";
            Properties.Settings.Default.ThemeSwitch = true;

            string selectedLightTheme = (string)ComboBoxLightTheme.SelectedItem;
            Properties.Settings.Default.ThemeLight = GetUserThemes().Where(t => t.Contains(selectedLightTheme)).FirstOrDefault();

            string selectedDarkTheme = (string)ComboBoxDarkTheme.SelectedItem;
            Properties.Settings.Default.ThemeDark = GetUserThemes().Where(t => t.Contains(selectedDarkTheme)).FirstOrDefault();

            buttonSaveTheme.IsEnabled = false;
            regEditHandler.SwitchThemeBasedOnTime();
        }

        private void ButtonDisableTheme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ThemeSwitch = false;
            Properties.Settings.Default.ThemeLight = null;
            Properties.Settings.Default.ThemeDark = null;
            buttonSaveTheme.IsEnabled = false;
            ComboBoxDarkTheme.SelectedIndex = -1;
            ComboBoxLightTheme.SelectedIndex = -1;
            theme1 = false;
            theme2 = false;
            cbSelection.SelectedIndex = 0;
        }

        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TbOpenThemeCP.Cursor = Mouse.OverrideCursor = Cursors.Hand;
        }

        private void TextBlock_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TbOpenThemeCP.Cursor = Mouse.OverrideCursor = null;
        }

        private async void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:themes"));
        }

        private List<string> GetUserThemes()
        {
            string themeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
            return Directory.EnumerateFiles(themeDirectory, "*.theme", SearchOption.TopDirectoryOnly).ToList();
        }

        private void ComboBoxDarkTheme_DropDownOpened(object sender, EventArgs e)
        {
            var themeNames = GetThemeFiles();
            ComboBoxDarkTheme.ItemsSource = themeNames;
        }

        private void ComboBoxLightTheme_DropDownOpened(object sender, EventArgs e)
        {
            var themeNames = GetThemeFiles();
            ComboBoxLightTheme.ItemsSource = themeNames;
        }

        private void ButtonOpenThemePath_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes");
        }

        private IEnumerable GetThemeFiles()
        {
            var themePaths = GetUserThemes();
            var themeNames = themePaths.Select(s => Path.GetFileNameWithoutExtension(s));
            return themeNames;
        }

        private void ComboBoxLightTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theme1 = true;
            EnableSaveButton();
        }

        private void ComboBoxDarkTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            theme2 = true;
            EnableSaveButton();
        }
    }
}
