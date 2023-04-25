#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeApp.Handlers;
using AutoDarkModeLib;
using AutoDarkModeLib.IThemeManager2;
using AutoDarkModeSvc.Communication;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AutoDarkModeLib.IThemeManager2.Flags;
using AdmProperties = AutoDarkModeLib.Properties;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageThemePicker.xaml
    /// </summary>
    public partial class PageThemePicker : ModernWpf.Controls.Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly string ThemeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Windows\Themes";
        private List<ThemeFile> themeCollection = ThemeCollectionHandler.GetUserThemes();
        private readonly bool init = true;
        private bool theme1 = false;
        private bool theme2 = false;

        public PageThemePicker()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "PageThemePicker - couldn't init config file");
            }

            InitializeComponent();
            UiHandler();

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                FontIconDark.FontFamily = new("Segoe Fluent Icons");
                FontIconLight.FontFamily = new("Segoe Fluent Icons");
                FontIconLink.FontFamily = new("Segoe Fluent Icons");
            }

            if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                IgnoreSettingsTextBlock.Visibility = Visibility.Collapsed;
                IgnoreSettingsCard.Visibility = Visibility.Collapsed;
            }

            init = false;
        }

        private void UiHandler()
        {
            //collapse changes saved message
            TextBlockUserFeedback.Text = AdmProperties.Resources.welcomeText;

            //give numbers to the steps
            TextBlockStep1.Text = AdmProperties.Resources.ThemeTutorialStep + " 1)";
            TextBlockStep2.Text = AdmProperties.Resources.ThemeTutorialStep + " 2)";
            TextBlockStep3.Text = AdmProperties.Resources.ThemeTutorialStep + " 3)";
            TextBlockStep4.Text = AdmProperties.Resources.ThemeTutorialStep + " 4)";

            //get all themes and select them in the combobox
            if (builder.Config.WindowsThemeMode.LightThemePath != null && builder.Config.WindowsThemeMode.DarkThemePath != null)
            {
                IEnumerable<string> themeNames = themeCollection.Select(t => t.ToString());
                ComboBoxDarkTheme.ItemsSource = themeNames;
                ComboBoxLightTheme.ItemsSource = themeNames;
                ThemeFile lightSelected = themeCollection.FirstOrDefault(t => t.Path == builder.Config.WindowsThemeMode.LightThemePath);
                ThemeFile darkSelected = themeCollection.FirstOrDefault(t => t.Path == builder.Config.WindowsThemeMode.DarkThemePath);


                if (lightSelected != null) ComboBoxLightTheme.SelectedItem = lightSelected.ToString();
                if (darkSelected != null) ComboBoxDarkTheme.SelectedItem = darkSelected.ToString();

                UpdateIgnoreStackPanel();

            }

            if (builder.Config.WindowsThemeMode.MonitorActiveTheme)
            {
                CheckBoxMonitorActiveTheme.IsChecked = true;
            }

            //check if Windows Theme Mode is enabled
            if (builder.Config.WindowsThemeMode.Enabled)
            {
                ToggleSwitchThemeMode.IsOn = true;
            }
            else
            {
                ComboBoxDarkTheme.IsEnabled = false;
                ComboBoxLightTheme.IsEnabled = false;
                theme1 = false;
                theme2 = false;
            }

            UpdateApplyFlagCheckBoxes();

        }

        private void UpdateApplyFlagCheckBoxes()
        {
            CheckBoxIgnoreBackground.IsChecked = false;
            CheckBoxIgnoreCursor.IsChecked = false;
            CheckBoxIgnoreDesktopIcons.IsChecked = false;
            CheckBoxIgnoreSound.IsChecked = false;

            builder.Config.WindowsThemeMode.ApplyFlags.ForEach(f =>
            {
                switch (f)
                {
                    case ThemeApplyFlags.IgnoreBackground:
                        CheckBoxIgnoreBackground.IsChecked = true;
                        break;
                    case ThemeApplyFlags.IgnoreCursor:
                        CheckBoxIgnoreCursor.IsChecked = true;
                        break;
                    case ThemeApplyFlags.IgnoreDesktopIcons:
                        CheckBoxIgnoreDesktopIcons.IsChecked = true;
                        break;
                    case ThemeApplyFlags.IgnoreSound:
                        CheckBoxIgnoreSound.IsChecked = true;
                        break;
                }
            });
        }

        private void UpdateIgnoreStackPanel()
        {
            ThemeFile lightThemeSelected = themeCollection.FirstOrDefault(t => t.Name == (string)ComboBoxLightTheme.SelectedItem);
            ThemeFile darkThemeSelected = themeCollection.FirstOrDefault(t => t.Name == (string)ComboBoxDarkTheme.SelectedItem);

            if (lightThemeSelected == null || darkThemeSelected == null || lightThemeSelected.IsWindowsTheme || darkThemeSelected.IsWindowsTheme)
            {
                StackPanelIgnoreSettings.IsEnabled = false;
                IgnoreDisableMessage.Visibility = Visibility.Visible;
                IgnoreDisableSeparator.Visibility = Visibility.Visible;
                if (!init) builder.Config.WindowsThemeMode.ApplyFlags.Clear();
                UpdateApplyFlagCheckBoxes();
            }
            else
            {
                StackPanelIgnoreSettings.IsEnabled = true;
                IgnoreDisableSeparator.Visibility = Visibility.Collapsed;
                IgnoreDisableMessage.Visibility = Visibility.Collapsed;
            }
        }

        private void DisableThemeMode()
        {
            ComboBoxDarkTheme.IsEnabled = false;
            ComboBoxLightTheme.IsEnabled = false;
            theme1 = false;
            theme2 = false;
            RequestThemeSwitch();
        }

        private void EnableThemeMode()
        {
            ComboBoxDarkTheme.IsEnabled = true;
            ComboBoxLightTheme.IsEnabled = true;
            if (!init && ComboBoxDarkTheme.SelectedItem != null && ComboBoxLightTheme.SelectedItem != null)
            {
                SaveThemeSettings();
            }
        }

        //save theme selection
        private void SaveThemeSettings()
        {
            //disable wallpaper switch
            builder.Config.WallpaperSwitch.Enabled = false;

            //get selected light theme file from combobox
            string selectedLightTheme = (string)ComboBoxLightTheme.SelectedItem;
            try
            {
                ThemeFile selected = themeCollection.FirstOrDefault(t => t.ToString().Contains(selectedLightTheme));
                if (selected != null) builder.Config.WindowsThemeMode.LightThemePath = selected.Path;
            }
            catch
            {
                theme1 = false;
                ComboBoxLightTheme.SelectedItem = null;
            }

            //get selected dark theme file from combobox
            string selectedDarkTheme = (string)ComboBoxDarkTheme.SelectedItem;
            try
            {
                ThemeFile selected = themeCollection.FirstOrDefault(t => t.ToString().Contains(selectedDarkTheme));
                if (selected != null) builder.Config.WindowsThemeMode.DarkThemePath = selected.Path;
            }
            catch
            {
                theme2 = false;
                ComboBoxDarkTheme.SelectedItem = null;
            }

            //enable theme mode
            builder.Config.WindowsThemeMode.Enabled = true;

            //ui changes
            TextBlockUserFeedback.Visibility = Visibility.Visible;
            TextBlockUserFeedback.Text = AdmProperties.Resources.msgChangesSaved;

            //apply theme
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "SaveThemeSettings");
            }
            RequestThemeSwitch();
            Trace.WriteLine("Windows Theme Mode settings were applied");
        }


        private async void RequestThemeSwitch()
        {
            string result = StatusCode.Err;
            try
            {
                result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
                if (result != StatusCode.Ok)
                {
                    throw new SwitchThemeException(result, "PageWallpaper");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, $"ZeroMQ returned err {result}");
            }
        }

        //display error message if something went wrong
        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = AdmProperties.Resources.ErrorMessageBox + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
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

        private void ToggleSwitchThemeMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitchThemeMode.IsOn == true)
            {
                EnableThemeMode();
            }
            else
            {
                if (!init)
                {
                    builder.Config.WindowsThemeMode.Enabled = false;

                    try
                    {
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex, "couldn't disable classic mode");
                    }
                }
                DisableThemeMode();
            }
        }

        //display all theme files of the theme folder while opening the dropdown menu
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            themeCollection = ThemeCollectionHandler.GetUserThemes();
            var themeNames = themeCollection.Select(t => t.ToString());
            ((ComboBox)sender).ItemsSource = themeNames;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).Name.Equals("ComboBoxLightTheme"))
            {
                Trace.WriteLine("Light windows theme was set");
                theme1 = true;
            }
            else if (((ComboBox)sender).Name.Equals("ComboBoxDarkTheme"))
            {
                Trace.WriteLine("Dark windows theme was set");
                theme2 = true;
            }

            UpdateIgnoreStackPanel();

            if ((theme1 || theme2) && !init)
            {
                SaveThemeSettings();
            }
        }

        private async void TextBlockOpenImmersiveControlPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:themes"));
        }

        private void TextBlockOpenImmersiveControlPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockOpenImmersiveControlPanel_MouseDown(this, null);
            }
        }

        //open theme folder. If there is no, create one.
        private void TextBlockOpenThemeFolder_MouseDown(object sender, MouseButtonEventArgs e)
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

        private void TextBlockOpenThemeFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockOpenThemeFolder_MouseDown(this, null);
            }
        }

        private void TextBlockBackButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Frame.Navigate(typeof(PagePersonalization), null, new DrillInNavigationTransitionInfo());
        }

        private void CheckBoxMonitorActiveTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb.IsChecked ?? false)
                {
                    builder.Config.WindowsThemeMode.MonitorActiveTheme = true;
                }
                else
                {
                    builder.Config.WindowsThemeMode.MonitorActiveTheme = false;
                }
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxMonitorActiveTheme");
            }
        }

        private void CheckBoxIgnoreFlag_Click(object sender, RoutedEventArgs e)
        {
            List<ThemeApplyFlags> flags = new();

            if (CheckBoxIgnoreBackground.IsChecked ?? false) flags.Add(ThemeApplyFlags.IgnoreBackground);
            if (CheckBoxIgnoreCursor.IsChecked ?? false) flags.Add(ThemeApplyFlags.IgnoreCursor);
            if (CheckBoxIgnoreDesktopIcons.IsChecked ?? false) flags.Add(ThemeApplyFlags.IgnoreDesktopIcons);
            if (CheckBoxIgnoreSound.IsChecked ?? false) flags.Add(ThemeApplyFlags.IgnoreSound);
            builder.Config.WindowsThemeMode.ApplyFlags = flags;

            try
            {
                builder.Save();
                TextBlockUserFeedback.Text = AdmProperties.Resources.msgChangesSaved;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxMonitorActiveTheme");
            }
        }
    }
}
