using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for PageWallpaper.xaml
    /// </summary>
    public partial class PageWallpaper : Page
    {
        RegeditHandler regEditHandler = new RegeditHandler();
        bool theme1 = false;
        bool theme2 = true;

        public PageWallpaper()
        {
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            if (!Properties.Settings.Default.ThemeInsteadOfWallpaper)
            {
                if (!Properties.Settings.Default.Enabled) BGWinButton.IsEnabled = false;
                ShowDeskBGStatus();
                cbSelection.SelectedIndex = 0;
            }
            else
            {
                cbSelection.SelectedIndex = 1;
                if (Properties.Settings.Default.ThemeSwitch)
                {
                    TextBoxDarkTheme.Text = Properties.Settings.Default.ThemeDark;
                    TextBoxLightTheme.Text = Properties.Settings.Default.ThemeLight;
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
            buttonLightTheme.Visibility = value;
            buttonDarkTheme.Visibility = value;
            buttonSaveTheme.Visibility = value;
            TextBoxDarkTheme.Visibility = value;
            TextBoxLightTheme.Visibility = value;
            ButtonDisableTheme.Visibility = value;
            TbOpenThemeCP.Visibility = value;
            TbTheme1.Visibility = value;
            TbTheme2.Visibility = value;
            TbTheme3.Visibility = value;
            TbTheme4.Visibility = value;
            TbTheme5.Visibility = value;
        }

        private void buttonSelectTheme_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "THEME FILE" + "|*.theme",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                if (((Button)sender).CommandParameter.ToString().Equals("LightTheme"))
                {
                    Properties.Settings.Default.ThemeLight = dlg.FileName;
                    TextBoxLightTheme.Text = dlg.FileName;
                    theme1 = true;
                }
                if (((Button)sender).CommandParameter.ToString().Equals("DarkTheme"))
                {
                    Properties.Settings.Default.ThemeDark = dlg.FileName;
                    TextBoxDarkTheme.Text = dlg.FileName;
                    theme2 = true;
                }
                EnableSaveButton();
            }
        }

        private void EnableSaveButton()
        {
            if(theme1 == true && theme2 == true)
            {
                buttonSaveTheme.IsEnabled = true;
            }
        }

        private void buttonSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WallpaperSwitch = false;
            Properties.Settings.Default.WallpaperDark = "";
            Properties.Settings.Default.WallpaperLight = "";
            Properties.Settings.Default.ThemeSwitch = true;
            Properties.Settings.Default.ThemeInsteadOfWallpaper = true;
            buttonSaveTheme.IsEnabled = false;
            regEditHandler.SwitchThemeBasedOnTime();
        }

        private void ButtonDisableTheme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ThemeSwitch = false;
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
    }
}
