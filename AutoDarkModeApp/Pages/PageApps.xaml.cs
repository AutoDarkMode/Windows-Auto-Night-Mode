using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using AutoDarkModeApp.Handlers;
using AutoDarkModeApp.Communication;
using AutoDarkModeSvc.Communication;
using AutoDarkModeConfig;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageApps.xaml
    /// </summary>
    public partial class PageApps : Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
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
            if (builder.Config.WindowsThemeMode.Enabled)
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
                if (!builder.Config.WindowsThemeMode.Enabled) AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;

                //is accent color switch enabled?
                AccentColorCheckBox.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorOnDark;
            }

            //combobox
            AppComboBox.SelectedIndex = (int)builder.Config.AppsSwitch.Component.Mode;
            SystemComboBox.SelectedIndex = (int)builder.Config.SystemSwitch.Component.Mode; ;
            if (builder.Config.Office.Enabled)
            {
                OfficeComboBox.SelectedIndex = (int)builder.Config.Office.Mode;
            }
            else
            {
                OfficeComboBox.SelectedIndex = 3;
            }


            //checkbox
            if (builder.Config.Office.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
            }
        }

        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            builder.Config.AppsSwitch.Enabled = true;
            if (AppComboBox.SelectedIndex.Equals(0))
            {
                builder.Config.AppsSwitch.Component.Mode = Mode.Switch;
            }
            else if (AppComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.AppsSwitch.Component.Mode = Mode.LightOnly;
            }
            else if (AppComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.AppsSwitch.Component.Mode = Mode.DarkOnly;
            }
            else if (AppComboBox.SelectedIndex.Equals(3))
            {
                builder.Config.AppsSwitch.Enabled = false;
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
            builder.Config.SystemSwitch.Enabled = true;
            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                builder.Config.SystemSwitch.Component.Mode = Mode.Switch;
                AccentColorCheckBox.IsEnabled = true;
            }
            else if (SystemComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.SystemSwitch.Component.Mode = Mode.LightOnly;
                AccentColorCheckBox.IsEnabled = false;
            }
            else if (SystemComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.SystemSwitch.Component.Mode = Mode.DarkOnly;
                AccentColorCheckBox.IsEnabled = true;
            }
            else if (SystemComboBox.SelectedIndex.Equals(3))
            {
                builder.Config.SystemSwitch.Enabled = false;
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
                builder.Config.SystemSwitch.Component.TaskbarColorOnDark = true;
            }
            else
            {
                builder.Config.SystemSwitch.Component.TaskbarColorOnDark = false;
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
            else if (OfficeComboBox.SelectedIndex.Equals(1))
            {
                builder.Config.Office.Mode = Mode.LightOnly;
            }
            else if (OfficeComboBox.SelectedIndex.Equals(2))
            {
                builder.Config.Office.Mode = Mode.DarkOnly;
            }
            else if (OfficeComboBox.SelectedIndex.Equals(3))
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
            Properties.Settings.Default.OfficeThemeChange = 3;
            OfficeComboBox.SelectedIndex = 3;
        }

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser");
        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxOfficeWhiteTheme.IsChecked ?? true)
            {
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
                if (result != Response.Ok)
                {
                    throw new SwitchThemeException(result, "PageApps");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }
    }
}