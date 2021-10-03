using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using AutoDarkModeApp.Handlers;
using AutoDarkModeSvc.Communication;
using AutoDarkModeConfig;
using AutoDarkModeComms;

namespace AutoDarkModeApp
{
    /// <summary>
    /// Interaction logic for PageApps.xaml
    /// </summary>
    public partial class PageApps : Page
    {
        private readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        readonly ICommandClient messagingClient = new ZeroMQClient(Address.DefaultPort);
        private bool is1903 = false;
        private readonly bool init = true;

        public PageApps()
        {
            InitializeComponent();
            UiHandler();
            init = false;
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

            //if a windows theme file was picked
            if (builder.Config.WindowsThemeMode.Enabled)
            {
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                AppComboBox.IsEnabled = false;
                AppComboBox.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
                NumberBoxColorDelay.IsEnabled = false;
                NumberBoxColorDelay.ToolTip = Properties.Resources.ToolTipDisabledDueTheme;
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

                //numbox
                NumberBoxColorDelay.Value = Convert.ToInt32(builder.Config.SystemSwitch.Component.TaskbarSwitchDelay);

                //is accent color switch enabled?
                AccentColorCheckBox.IsChecked = builder.Config.SystemSwitch.Component.TaskbarColorOnDark;
                if (!AccentColorCheckBox.IsChecked.Value)
                {
                    TextBlockColorDelay.Visibility = Visibility.Collapsed;
                    NumberBoxColorDelay.Visibility = Visibility.Collapsed;
                }
            }

            //combobox
            if (builder.Config.AppsSwitch.Enabled)
            {
                AppComboBox.SelectedIndex = (int)builder.Config.AppsSwitch.Component.Mode;
            }
            else
            {
                AppComboBox.SelectedIndex = 3;
            }

            if (builder.Config.SystemSwitch.Enabled)
            {
                SystemComboBox.SelectedIndex = (int)builder.Config.SystemSwitch.Component.Mode;
            }
            else
            {
                SystemComboBox.SelectedIndex = 3;
            }

            if (builder.Config.OfficeSwitch.Enabled)
            {
                OfficeComboBox.SelectedIndex = (int)builder.Config.OfficeSwitch.Component.Mode;
            }
            else
            {
                OfficeComboBox.SelectedIndex = 3;
            }


            //checkbox
            if (builder.Config.OfficeSwitch.Component.LightTheme == 5)
            {
                CheckBoxOfficeWhiteTheme.IsChecked = true;
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

        private async void RequestThemeSwitch()
        {
            try
            {
                string result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch, 15);
                if (result != StatusCode.Ok)
                {
                    throw new SwitchThemeException(result, "PageApps");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void AppComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
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
        }

        private void SystemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
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
        }

        private void AccentColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked ?? false)
            {
                builder.Config.SystemSwitch.Component.TaskbarColorOnDark = true;
                TextBlockColorDelay.Visibility = Visibility.Visible;
                NumberBoxColorDelay.Visibility = Visibility.Visible;
            }
            else
            {
                builder.Config.SystemSwitch.Component.TaskbarColorOnDark = false;
                TextBlockColorDelay.Visibility = Visibility.Collapsed;
                NumberBoxColorDelay.Visibility = Visibility.Collapsed;
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

        private void NumberBoxColorDelay_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                builder.Config.SystemSwitch.Component.TaskbarSwitchDelay = Convert.ToInt32(NumberBoxColorDelay.Value);
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }
        }

        private void OfficeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                builder.Config.OfficeSwitch.Enabled = true;
                if (OfficeComboBox.SelectedIndex.Equals(0))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.Switch;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(1))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.LightOnly;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(2))
                {
                    builder.Config.OfficeSwitch.Component.Mode = Mode.DarkOnly;
                }
                else if (OfficeComboBox.SelectedIndex.Equals(3))
                {
                    builder.Config.OfficeSwitch.Enabled = false;
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

        }

        private void CheckBoxOfficeWhiteTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxOfficeWhiteTheme.IsChecked ?? true)
            {
                builder.Config.OfficeSwitch.Component.LightTheme = 5;
            }
            else
            {
                builder.Config.OfficeSwitch.Component.LightTheme = 0;
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

        private void ButtonWikiBrowserExtension_Click(object sender, RoutedEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Dark-Mode-for-Webbrowser");
        }

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
