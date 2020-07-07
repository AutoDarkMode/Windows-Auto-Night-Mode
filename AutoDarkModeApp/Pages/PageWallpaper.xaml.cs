using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageWallpaper.xaml
    /// </summary>
    public partial class PageWallpaper : Page
    {
        readonly RegeditHandler regEditHandler = new RegeditHandler();
        bool theme1 = false;
        bool theme2 = false;
        readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";

        public PageWallpaper()
        {
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            //if auto dark mode wasn't configured
            if (!Properties.Settings.Default.Enabled)
            {
                BGWinButton.IsEnabled = false;
                ComboBoxLightTheme.IsEnabled = false;
                ComboBoxDarkTheme.IsEnabled = false;
                ButtonSaveTheme.IsEnabled = false;
                ButtonDisableTheme.IsEnabled = false;
            }

            //give numbers to the steps
            TextBlockStep1.Text = Properties.Resources.ThemeTutorialStep + " 1)";
            TextBlockStep2.Text = Properties.Resources.ThemeTutorialStep + " 2)";
            TextBlockStep3.Text = Properties.Resources.ThemeTutorialStep + " 3)";
            TextBlockStep4.Text = Properties.Resources.ThemeTutorialStep + " 4)";

            //if theme switcher isn't enabled
            if (!Properties.Settings.Default.ThemeSwitch)
            {
                ComboBoxModeSelection.SelectedIndex = 0;
                ShowDeskBGStatus();
            }
            else
            {
                ComboBoxModeSelection.SelectedIndex = 1;
                if (Properties.Settings.Default.Enabled && Properties.Settings.Default.ThemeSwitch && Properties.Settings.Default.ThemeDark != null && Properties.Settings.Default.ThemeLight != null)
                {
                    var themeNames = GetThemeFiles();
                    ComboBoxDarkTheme.ItemsSource = themeNames;
                    ComboBoxLightTheme.ItemsSource = themeNames;
                    ComboBoxLightTheme.SelectedItem = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ThemeLight);
                    ComboBoxDarkTheme.SelectedItem = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ThemeDark);
                    theme1 = true;
                    theme2 = true;
                    ButtonSaveTheme.IsEnabled = true;
                }
            }
        }

        private void ComboBoxModeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxModeSelection.SelectedIndex == 0)
            {
                SetWallpaperVisibility(Visibility.Visible);
                SetThemeVisibility(Visibility.Collapsed);
            }
            else if (ComboBoxModeSelection.SelectedIndex == 1)
            {
                SetWallpaperVisibility(Visibility.Collapsed);
                SetThemeVisibility(Visibility.Visible);
            }
        }

        private void SetThemeVisibility(Visibility value)
        {
            ComboBoxDarkTheme.Visibility = value;
            ComboBoxLightTheme.Visibility = value;
            StackPanelThemeButtons.Visibility = value;
            ButtonOpenThemePath.Visibility = value;
            ButtonOpenThemeImmersiveControlPanel.Visibility = value;
            TbTheme1.Visibility = value;
            TbTheme2.Visibility = value;
            TbTheme3.Visibility = value;
            TbTheme4.Visibility = value;
            StackPanelTutorial.Visibility = value;
            IconDark.Visibility = value;
            IconLight.Visibility = value;
        }
        private void SetWallpaperVisibility(Visibility value)
        {
            wallpaperHeading.Visibility = value;
            wallpaperIcon.Visibility = value;
            wallpaperCurrent.Visibility = value;
            DeskBGStatus.Visibility = value;
            BGWinButton.Visibility = value;
            StackPanelWallpaperStatus.Visibility = value;
        }

        //show if wallpaper switch is enabled
        private void ShowDeskBGStatus()
        {
            if (Properties.Settings.Default.WallpaperSwitch)
            {
                DeskBGStatus.Text = Properties.Resources.enabled;
            }
            else
            {
                DeskBGStatus.Text = Properties.Resources.disabled;
            }
        }
        //open wallpaper configuration
        private void BGWinButton_Click(object sender, RoutedEventArgs e)
        {
            DesktopBGui BGui = new DesktopBGui();
            BGui.Owner = Window.GetWindow(this);
            BGui.ShowDialog();
            if (BGui.saved == true)
            {
                ButtonDisableTheme_Click(this, e);
                regEditHandler.SwitchThemeBasedOnTime();
            }
            ShowDeskBGStatus();
        }

        //save theme selection
        private void ButtonSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            //disable auto dark mode wallpaper switch
            Properties.Settings.Default.WallpaperSwitch = false;
            Properties.Settings.Default.WallpaperDark = "";
            Properties.Settings.Default.WallpaperLight = "";
            Properties.Settings.Default.ThemeSwitch = true;

            //get selected light theme file from combobox
            string selectedLightTheme = (string)ComboBoxLightTheme.SelectedItem;
            Properties.Settings.Default.ThemeLight = GetUserThemes().Where(t => t.Contains(selectedLightTheme)).FirstOrDefault();

            //get selected dark theme file from combobox
            string selectedDarkTheme = (string)ComboBoxDarkTheme.SelectedItem;
            Properties.Settings.Default.ThemeDark = GetUserThemes().Where(t => t.Contains(selectedDarkTheme)).FirstOrDefault();

            //ui changes
            ButtonSaveTheme.IsEnabled = false;

            //apply theme
            try
            {
                regEditHandler.SwitchThemeBasedOnTime();
            }
            catch
            {
                Properties.Settings.Default.ThemeSwitch = false;
                MsgBox msg = new MsgBox(Properties.Resources.ThemeApplyError1, Properties.Resources.errorOcurredTitle, "error", "close");
                msg.Owner = Window.GetWindow(this);
                msg.ShowDialog();
            }
        }

        //disable theme file switch
        private void ButtonDisableTheme_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ThemeSwitch = false;
            Properties.Settings.Default.ThemeLight = null;
            Properties.Settings.Default.ThemeDark = null;

            ButtonSaveTheme.IsEnabled = false;
            theme1 = false;
            theme2 = false;

            ComboBoxDarkTheme.SelectedIndex = -1;
            ComboBoxLightTheme.SelectedIndex = -1;
            ComboBoxModeSelection.SelectedIndex = 0;
        }

        //get a list of all files the theme folder contains. If there is no theme-folder, create one.
        private List<string> GetUserThemes()
        {
            try
            {
                return Directory.EnumerateFiles(ThemeFolderPath, "*.theme", SearchOption.TopDirectoryOnly).ToList();
            }
            catch
            {
                Directory.CreateDirectory(ThemeFolderPath);
                return GetUserThemes();
            }
        }
        //convert list of all files to a list of file names without file extension
        private IEnumerable GetThemeFiles()
        {
            var themePaths = GetUserThemes();
            var themeNames = themePaths.Select(s => Path.GetFileNameWithoutExtension(s));
            return themeNames;
        }

        //display all theme files in the theme folder while opening the dropdown menu
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        { 
            var themeNames = GetThemeFiles();
            ((ComboBox)sender).ItemsSource = themeNames;
        }

        //enable save button only, if the user choosed two themes
        private void EnableSaveButton()
        {
            if (theme1 == true && theme2 == true)
            {
                ButtonSaveTheme.IsEnabled = true;
            }
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Name.Equals("ComboBoxLightTheme"))
            {
                theme1 = true;
                EnableSaveButton();
            }
            else if (((ComboBox)sender).Name.Equals("ComboBoxDarkTheme"))
            {
                theme2 = true;
                EnableSaveButton();
            }
        }

        //open windows uwp settings to let the user create a theme file there
        private async void ButtonOpenThemeImmersiveControlPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:themes"));
        }
        private void ButtonOpenThemeImmersiveControlPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ButtonOpenThemeImmersiveControlPanel_MouseDown(this, null);
        }

        //open theme folder. If there is no, create one.
        private void ButtonOpenThemePath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(ThemeFolderPath);
            }
            catch
            {
                Directory.CreateDirectory(ThemeFolderPath);
                Process.Start(ThemeFolderPath);
            }
        }
        private void ButtonOpenThemePath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ButtonOpenThemePath_Click(this, null);
        }
    }
}
