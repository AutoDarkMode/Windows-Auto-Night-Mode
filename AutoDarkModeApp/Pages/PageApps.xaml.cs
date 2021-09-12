using AutoDarkModeSvc.Config;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoDarkModeSvc;
using System.Diagnostics;
using AutoDarkModeApp.Handlers;
using AutoDarkModeApp.Communication;
using AutoDarkModeSvc.Communication;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageApps.xaml
    /// </summary>
    public partial class PageApps : Page
    {
        private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        readonly ICommandClient messagingClient = new ZeroMQClient(Command.DefaultPort);
        bool is1903 = false;

        public PageApps()
        {
            builder.Load();
            InitializeComponent();
            UiHandler();

            //follow windows theme
            ThemeChange(this, null);
            SourceChord.FluentWPF.SystemTheme.ThemeChanged += ThemeChange;
        }

        //react to windows theme change
        // still required??? @Armin2208
        private void ThemeChange(object sender, EventArgs e)
        {
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark))
            {
            }
            else
            {
            }
        }

        private void UiHandler()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            //if automatic theme switch isn't enabled
            if (!builder.Config.AutoThemeSwitchingEnabled)
            {
                AccentColorCheckBox.IsEnabled = false;
                SystemComboBox.IsEnabled = false;
                AppComboBox.IsEnabled = false;
                OfficeComboBox.IsEnabled = false;
                CheckBoxOfficeWhiteTheme.IsEnabled = false;
            }

            //if a windows theme file was picked
            if (!builder.Config.ClassicMode)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                AppComboBox.IsEnabled = false;
                AppComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
            }

            //if the OS version is older than 1903
            if (int.Parse(RegistryHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
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
                if(builder.Config.ClassicMode) AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;

                //is accent color switch enabled?
                AccentColorCheckBox.IsChecked = builder.Config.AccentColorTaskbarEnabled;
            }

            //combobox
            AppComboBox.SelectedIndex = (int)builder.Config.AppsTheme;
            SystemComboBox.SelectedIndex = (int)builder.Config.SystemTheme;
            if (builder.Config.Office.Enabled)
            {
                OfficeComboBox.SelectedIndex = (int)builder.Config.Office.Mode;
            }
            else
            {
                OfficeComboBox.SelectedIndex = 4;
            }


            //checkbox
            if (builder.Config.Office.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
            }
        }

        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (AppComboBox.SelectedIndex.Equals(0))
            {
                builder.Config.AppsTheme = Mode.Switch;
            }

            if (AppComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.AppsTheme = Mode.LightOnly;
            }

            if (AppComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.AppsTheme = Mode.DarkOnly;
            }

            if (AppComboBox.SelectedIndex.Equals(3))
            {
                builder.Config.AppsTheme = Mode.Bluelight;
            }
            try
            {
                builder.Save();
            } 
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void SystemComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                builder.Config.SystemTheme = Mode.Switch;
                AccentColorCheckBox.IsEnabled = true;
            }

            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.SystemTheme = Mode.LightOnly;
                AccentColorCheckBox.IsEnabled = false;
            }

            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.SystemTheme = Mode.DarkOnly;
                Properties.Settings.Default.SystemThemeChange = 2;
                AccentColorCheckBox.IsEnabled = true;
            }

            if (SystemComboBox.SelectedIndex.Equals(3))
            {
                builder.Config.SystemTheme = Mode.Bluelight;
                AccentColorCheckBox.IsEnabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void AccentColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked ?? false)
            {
                builder.Config.AccentColorTaskbarEnabled = true;
            }
            else
            {
                builder.Config.AccentColorTaskbarEnabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }

        private void OfficeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            builder.Config.Office.Enabled = true;
            if (OfficeComboBox.SelectedIndex.Equals(0))
            {
                builder.Config.Office.Mode = Mode.Switch;
            }

            if (OfficeComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.Office.Mode = Mode.LightOnly;
            }

            if (OfficeComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.Office.Mode = Mode.DarkOnly;
            }

            if (OfficeComboBox.SelectedIndex.Equals(3))
            {
                builder.Config.Office.Mode = Mode.Bluelight;
            }

            if (OfficeComboBox.SelectedIndex.Equals(4))
            {
                builder.Config.Office.Enabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            RequestThemeSwitch();
        }
        private void DisableOfficeSwitch()
        {
            //does nothing for now
            Properties.Settings.Default.OfficeThemeChange = 4;
            OfficeComboBox.SelectedIndex = 4;
        }

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser");
        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            if(CheckBoxOfficeWhiteTheme.IsChecked ?? true){
                builder.Config.Office.LightTheme = 5;
                OfficeComboBox_DropDownClosed(this, null);
            }
            else
            {
                builder.Config.Office.LightTheme = 0;
                OfficeComboBox_DropDownClosed(this, null);
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void ShowErrorMessage(Exception ex)
        {
            string error = Properties.Resources.errorThemeApply + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
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

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private async void RequestThemeSwitch()
        {
            try
            {
                string result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch);
                if (result == Response.Err)
                {
                    throw new SwitchThemeException();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
    }
}