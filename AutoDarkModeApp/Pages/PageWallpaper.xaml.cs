using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Handlers;
using AutoDarkModeConfig;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageWallpaper.xaml
    /// </summary>
    public partial class PageWallpaper : Page
    {
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        bool theme1 = false;
        bool theme2 = false;
        readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
        readonly ICommandClient messagingClient = new ZeroMQClient(Command.DefaultPort);

        public PageWallpaper()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("couldn't init config file", ex);
            }
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {

            //give numbers to the steps
            TextBlockStep1.Text = Properties.Resources.ThemeTutorialStep + " 1)";
            TextBlockStep2.Text = Properties.Resources.ThemeTutorialStep + " 2)";
            TextBlockStep3.Text = Properties.Resources.ThemeTutorialStep + " 3)";
            TextBlockStep4.Text = Properties.Resources.ThemeTutorialStep + " 4)";

            //if theme switcher isn't enabled
            if (!builder.Config.WindowsThemeMode.Enabled)
            {
                ComboBoxModeSelection.SelectedIndex = 0;
                ShowDeskBGStatus();
            }
            else
            {
                ComboBoxModeSelection.SelectedIndex = 1;
            }
            if (builder.Config.WindowsThemeMode.LightThemePath != null && builder.Config.WindowsThemeMode.DarkThemePath != null)
            {
                var themeNames = GetThemeFiles();
                ComboBoxDarkTheme.ItemsSource = themeNames;
                ComboBoxLightTheme.ItemsSource = themeNames;
                ComboBoxLightTheme.SelectedItem = Path.GetFileNameWithoutExtension(builder.Config.WindowsThemeMode.LightThemePath);
                ComboBoxDarkTheme.SelectedItem = Path.GetFileNameWithoutExtension(builder.Config.WindowsThemeMode.DarkThemePath);
                theme1 = true;
                theme2 = true;
                ButtonSaveTheme.IsEnabled = true;
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
            WallpaperPositionComboBox.Visibility = value;
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
            if (builder.Config.WallpaperSwitch.Enabled)
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
                builder.Config.WindowsThemeMode.Enabled = false;
                ButtonSaveTheme.IsEnabled = false;
                ComboBoxModeSelection.SelectedIndex = 0;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("error saving config in BGWinButton_Click: ", ex);
            }
            ShowDeskBGStatus();
            RequestThemeSwitch();
        }

        //save theme selection
        private void ButtonSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            //disable auto dark mode wallpaper switch
            //TODO: needs adaption for new wallpaper handler
            //builder.Config.Wallpaper.Enabled = false;
            builder.Config.WindowsThemeMode.Enabled = true;

            //get selected light theme file from combobox
            string selectedLightTheme = (string)ComboBoxLightTheme.SelectedItem;
            builder.Config.WindowsThemeMode.LightThemePath = GetUserThemes().Where(t => t.Contains(selectedLightTheme)).FirstOrDefault();

            //get selected dark theme file from combobox
            string selectedDarkTheme = (string)ComboBoxDarkTheme.SelectedItem;
            builder.Config.WindowsThemeMode.DarkThemePath = GetUserThemes().Where(t => t.Contains(selectedDarkTheme)).FirstOrDefault();

            //ui changes
            ButtonSaveTheme.IsEnabled = false;

            //apply theme
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("couldn't save themes", ex);
            }
            RequestThemeSwitch();
        }

        //disable theme file switch
        private void ButtonDisableTheme_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.WindowsThemeMode.Enabled = false;
            ButtonSaveTheme.IsEnabled = false;
            theme1 = false;
            theme2 = false;
            ComboBoxModeSelection.SelectedIndex = 0;
            try
            {
                builder.Config.WindowsThemeMode.Enabled = false;
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("couldn't disable classic mode", ex);
            }
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
                Process.Start(new ProcessStartInfo(ThemeFolderPath)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch
            {
                Directory.CreateDirectory(ThemeFolderPath);
                Process.Start(new ProcessStartInfo(ThemeFolderPath)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        private void ButtonOpenThemePath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ButtonOpenThemePath_Click(this, null);
        }

        private async void RequestThemeSwitch()
        {
            string result = Response.Err;
            try
            {
                result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch);
                if (result != Response.Ok)
                {
                    throw new SwitchThemeException(result, "PageWallpaper");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"ZeroMQ returned err {result}", ex);
            }
        }

        private void ShowErrorMessage(String message, Exception ex)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\n{message}: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }

        private void WallpaperPosition_DropDownClosed(object sender, EventArgs e)
        {
            builder.Config.WallpaperSwitch.Component.Position = (WallpaperPosition)WallpaperPositionComboBox.SelectedIndex;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("couldn't save themes", ex);
            }
        }
    }
}
