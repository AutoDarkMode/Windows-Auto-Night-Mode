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
                AppComboBox.IsEnabled = false;
                CheckBoxOfficeWhiteTheme.IsEnabled = false;
            }

            //if a windows theme file was picked
            if (!builder.Config.ClassicMode)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                AppComboBox.IsEnabled = false;
                AppComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
            }

            //if the OS version is older than 1903
            if (int.Parse(RegistryHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            if (!is1903)
            {
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

            AppComboBox_DropDownClosed(this, null);
            //checkbox
            if (builder.Config.Office.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
            }

            AppCheckbox_Clicked(this, null);
        }

        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            Mode mode;
            EnableAppRadio();
            if (AppComboBox.SelectedItem.Equals(SystemInterface))
            {
                mode = builder.Config.SystemTheme;
                AppEnableTheming.IsEnabled = false;
                AppEnableTheming.IsChecked = true;
            }
            else if (AppComboBox.SelectedItem.Equals(SystemApps))
            {
                mode = builder.Config.AppsTheme;
                AppEnableTheming.IsEnabled = false;
                AppEnableTheming.IsChecked = true;
            }
            else if (AppComboBox.SelectedItem.Equals(MicrosoftOffice))
            {
                mode = builder.Config.Office.Mode;
                AppEnableTheming.IsEnabled = true;
                AppEnableTheming.IsChecked = builder.Config.Office.Enabled;
                if (!builder.Config.Office.Enabled) DisableAppRadio();
            }
            else if (AppComboBox.SelectedItem.Equals(Edge))
            {
                mode = builder.Config.EdgeTheme;
                AppEnableTheming.IsEnabled = false;
                AppEnableTheming.IsChecked = true;
            }
            else
            {
                ShowErrorMessage(new Exception("Unknown Apps dropdown element found"));
                mode = Mode.Switch;
                DisableAppRadio();
            }

            switch (mode)
            {
                case Mode.Switch:
                    AdaptiveRadio.IsChecked = true;
                    break;
                case Mode.DarkOnly:
                    DarkRadio.IsChecked = true;
                    break;
                case Mode.LightOnly:
                    LightRadio.IsChecked = true;
                    break;
                default:
                    ShowErrorMessage(new Exception("Unknown theme mode found in settings: " + mode));
                    break;
            }
        }
        
        private void AppRadio_Clicked(object sender, EventArgs e)
        {
            Mode mode;
            if (AdaptiveRadio.IsChecked ?? true)
            {
                mode = Mode.Switch;
            }
            else if (DarkRadio.IsChecked ?? true)
            {
                mode = Mode.DarkOnly;
            }
            else if (LightRadio.IsChecked ?? true)
            {
                mode = Mode.LightOnly;
            }
            else
            {
                ShowErrorMessage(new Exception("No theme option was selected after clicking"));
                return;
            }

            if (AppComboBox.SelectedItem.Equals(SystemInterface))
            {
                builder.Config.SystemTheme = mode;
            }
            else if (AppComboBox.SelectedItem.Equals(SystemApps))
            {
                builder.Config.AppsTheme = mode;
            }
            else if (AppComboBox.SelectedItem.Equals(MicrosoftOffice))
            {
                builder.Config.Office.Mode = mode;
            }
            else if (AppComboBox.SelectedItem.Equals(Edge))
            {
                builder.Config.EdgeTheme = mode;
            }
            else
            {
                ShowErrorMessage(new Exception("Unknown Apps dropdown element found"));
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

        private void AppCheckbox_Clicked(object sender, EventArgs e)
        {
            if ((bool) AppEnableTheming.IsChecked)
            {
                if (AppComboBox.SelectedItem.Equals(MicrosoftOffice)) builder.Config.Office.Enabled = true;
                EnableAppRadio();
            }
            else
            {
                if (AppComboBox.SelectedItem.Equals(MicrosoftOffice)) builder.Config.Office.Enabled = false;
                DisableAppRadio();
            }

            if (builder.Config.Office.Enabled)
            {
                CheckBoxOfficeWhiteTheme.IsEnabled = true;
                CheckBoxOfficeWhiteTheme.ToolTip = null;
            }
            else
            {
                CheckBoxOfficeWhiteTheme.IsEnabled = false;
                CheckBoxOfficeWhiteTheme.ToolTip = Properties.Resources.ToolTipDisabledDueOfficeDisabled;
            }
        }

        private void DisableAppRadio()
        {
            AdaptiveRadio.IsEnabled = false;
            DarkRadio.IsEnabled = false;
            LightRadio.IsEnabled = false;
        }

        private void EnableAppRadio()
        {
            AdaptiveRadio.IsEnabled = true;
            DarkRadio.IsEnabled = true;
            LightRadio.IsEnabled = true;
        }

        private void AccentColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.AccentColorTaskbarEnabled = ((CheckBox)sender).IsChecked ?? false;
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

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser");
        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.Office.LightTheme = (byte)(CheckBoxOfficeWhiteTheme.IsChecked ?? true ? 5 : 0);
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