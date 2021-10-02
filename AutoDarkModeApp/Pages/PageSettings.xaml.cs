using AutoDarkModeApp.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using AutoDarkModeComms;
using AutoDarkModeApp.Handlers;
using Windows.System.Power;
using System.IO;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        readonly string curLanguage = Settings.Default.Language;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        readonly ICommandClient messagingClient = new ZeroMQClient(Address.DefaultPort);
        private readonly bool init = true;
        readonly Updater updater = new();
        private readonly string BetaVersionQueryURL = @"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml";

        public PageSettings()
        {
            try
            {
                builder.Load();
                builder.LoadUpdaterData();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "PageSettings");
            }
            InitializeComponent();
            UiHandler();
            init = false;
        }
        private void UiHandler()
        {
            //disable elements which aren't compatible with this device
            if (PowerManager.BatteryStatus == BatteryStatus.NotPresent)
            {
                CheckBoxEnergySaverMitigation.IsEnabled = false;
            }

            //language ui
            ButtonRestart.Visibility = Visibility.Collapsed;
            TextBlockLanguageRestart.Visibility = Visibility.Collapsed;
            ComboBoxLanguageSelection.SelectedValue = Settings.Default.Language.ToString();
            if (ComboBoxLanguageSelection.SelectedValue == null)
            {
                ComboBoxLanguageSelection.SelectedValue = "en";
            }


            CheckBoxAlterTime.IsChecked = Settings.Default.AlterTime;
            CheckBoxLogonTask.IsChecked = builder.Config.Tunable.UseLogonTask;
            CheckBoxHideTrayIcon.IsChecked = !builder.Config.Tunable.ShowTrayIcon;
            CheckBoxColourFilter.IsChecked = builder.Config.ColorFilterSwitch.Enabled;


            //battery slider / energy saver mitigation
            BatterySlider.Value = builder.Config.Tunable.BatterySliderDefaultValue;
            CheckBoxEnergySaverMitigation.ToolTip = Properties.Resources.cbSettingsEnergySaverMitigationInfo;
            if (!builder.Config.Tunable.DisableEnergySaverOnThemeSwitch)
            {
                StackPanelBatterySlider.Visibility = Visibility.Collapsed;
            }
            else
            {
                CheckBoxEnergySaverMitigation.IsChecked = true;
            }

            //updater ui
            if (builder.UpdaterData.LastCheck.Year.ToString().Equals("1"))
            {
                TextBlockUpdateInfo.Text = "Last checked: " + "never";
            }
            else
            {
                TextBlockUpdateInfo.Text = "Last checked: " + builder.UpdaterData.LastCheck.ToString();
            }
            CheckBoxEnableUpdater.IsChecked = builder.Config.Updater.Enabled;
            switch (builder.Config.Updater.DaysBetweenUpdateCheck)
            {
                case 3:
                    ComboBoxDaysBetweenUpdateCheck.SelectedItem = ComboBoxDaysBetweenUpdateCheck3Days;
                    break;
                case 7:
                    ComboBoxDaysBetweenUpdateCheck.SelectedItem = ComboBoxDaysBetweenUpdateCheck7Days;
                    break;
                case 14:
                    ComboBoxDaysBetweenUpdateCheck.SelectedItem = ComboBoxDaysBetweenUpdateCheck14Days;
                    break;
            }
            CheckBoxAutoInstall.IsChecked = builder.Config.Updater.AutoInstall;
            CheckBoxUpdateSilent.IsChecked = builder.Config.Updater.Silent;
            if (!CheckBoxAutoInstall.IsChecked.Value) CheckBoxUpdateSilent.IsEnabled = false;
            if(String.IsNullOrEmpty(builder.Config.Updater.VersionQueryUrl))
            {
                RadioButtonStableUpdateChannel.IsChecked = true;
            }
            else if (builder.Config.Updater.VersionQueryUrl.Equals(BetaVersionQueryURL))
            {
                RadioButtonBetaUpdateChannel.IsChecked = true;
            }
            else
            {
                RadioButtonBetaUpdateChannel.IsEnabled = false;
                RadioButtonStableUpdateChannel.IsEnabled = false;
            }
        }

        private void ComboBoxLanguageSelection_DropDownClosed(object sender, System.EventArgs e)
        {
            string selectedLanguage = ComboBoxLanguageSelection.SelectedValue.ToString();
            if (selectedLanguage != curLanguage)
            {
                SetLanguage(selectedLanguage);
                Translator.Text = Properties.Resources.lblTranslator;
                TextBlockLanguageRestart.Text = Properties.Resources.restartNeeded;
                TextBlockLanguageRestart.Visibility = Visibility.Visible;
                ButtonRestart.Content = Properties.Resources.restart;
                ButtonRestart.Visibility = Visibility.Visible;
                Settings.Default.LanguageChanged = true;
            }
            else
            {
                SetLanguage(selectedLanguage);
                TextBlockLanguageRestart.Visibility = Visibility.Collapsed;
                ButtonRestart.Visibility = Visibility.Collapsed;
                Translator.Text = Properties.Resources.lblTranslator;
                Settings.Default.LanguageChanged = false;
            }
        }

        private static void SetLanguage(string lang)
        {
            Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language);
        }

        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Default.Save();
                Process.Start(new ProcessStartInfo(Extensions.ExecutionPathApp)
                {
                    UseShellExecute = false,
                    Verb = "open"
                });
                Application.Current.Shutdown();
            }
            catch
            {

            }
        }

        private void CheckBoxAlterTime_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxAlterTime.IsChecked.Value)
            {
                Settings.Default.AlterTime = true;
            }
            else
            {
                Settings.Default.AlterTime = false;
            }
        }

        private async void CheckBoxColourFilter_Click(object sender, RoutedEventArgs e)
        {
            if(CheckBoxColourFilter.IsChecked.Value)
            {
                builder.Config.ColorFilterSwitch.Enabled = true;
            }
            else
            {
                builder.Config.ColorFilterSwitch.Enabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxColourFilter_Click");
            }
            _ = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch);
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
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

        private void BatterySlider_Save(object sender, EventArgs e)
        {
            builder.Config.Tunable.BatterySliderDefaultValue = (int)BatterySlider.Value;
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "BatterySlider_Save");
            }
        }

        private void CheckBoxEnergySaverMitigation_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnergySaverMitigation.IsChecked.Value)
            {
                builder.Config.Tunable.DisableEnergySaverOnThemeSwitch = true;
                StackPanelBatterySlider.Visibility =  Visibility.Visible;
            }
            else
            {
                builder.Config.Tunable.DisableEnergySaverOnThemeSwitch = false;
                StackPanelBatterySlider.Visibility = Visibility.Collapsed;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxEnergySaverMitigation_Click");
            }
        }

        private async void CheckBoxLogonTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckBoxLogonTask.IsChecked.Value)
                {
                    builder.Config.Tunable.UseLogonTask = true;
                }
                else
                {
                    builder.Config.Tunable.UseLogonTask = false;
                }
                builder.Save();

                if (builder.Config.AutoThemeSwitchingEnabled)
                {
                    var result = await messagingClient.SendMessageAndGetReplyAsync(Command.AddAutostart);
                    if (result != StatusCode.Ok)
                    {
                        throw new AddAutoStartException($"error creating auto start task, ZMQ returned {result}", "AutoDarkModeSvc.MessageParser.AddAutostart");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxLogonTask_Click");
            }
        }

        private void CheckBoxHideTrayIcon_Click(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).IsChecked.Value)
            {
                MsgBox confirm = new("Disabling the icon isn't recommended. Do you really want to continue?", "Hide Tray Icon", "info", "yesno")
                {
                    Owner = Window.GetWindow(this)
                };
                bool result = confirm.ShowDialog() ?? false;
                if (!result)
                {
                    (sender as CheckBox).IsChecked = false;
                    return;
                }
                builder.Config.Tunable.ShowTrayIcon = false;
            }
            else
            {
                builder.Config.Tunable.ShowTrayIcon = true;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxHideTrayIcon_Click");
            }
        }

        /// <summary>
        /// Updater
        /// </summary>
        private void ButtonSearchUpdate_Click(object sender, RoutedEventArgs e)
        {
            ButtonSearchUpdate.IsEnabled = false;
            TextBlockUpdateInfo.Text = Properties.Resources.msgSearchUpd;//searching for update...
            updater.CheckNewVersion();
            if (updater.UpdateAvailable())
            {
                TextBlockUpdateInfo.Text = Properties.Resources.msgUpdateAvail;//a new update is available!
            }
            else
            {
                TextBlockUpdateInfo.Text = Properties.Resources.msgNoUpd;//no new updates are available.
                ButtonSearchUpdate.IsEnabled = true;
            }
        }

        private void CheckBoxEnableUpdater_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Updater.Enabled = true;
            }
            else
            {
                builder.Config.Updater.Enabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxEnableUpdater_Click");
            }
        }

        private void ComboBoxDaysBetweenUpdateCheck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                switch((sender as ComboBox).SelectedIndex)
                {
                    case 0:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 3;
                        break;
                    case 1:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 7;
                        break;
                    case 2:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 14;
                        break;
                }
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "ComboBoxDaysBetweenUpdateCheck_SelectionChanged");
                }
            }
        }

        private void CheckBoxAutoInstall_Click(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Updater.AutoInstall = true;
                CheckBoxUpdateSilent.IsEnabled = true;
            }
            else
            {
                builder.Config.Updater.AutoInstall = false;
                builder.Config.Updater.Silent = false;
                CheckBoxUpdateSilent.IsChecked = false;
                CheckBoxUpdateSilent.IsEnabled = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxAutoInstall_Click");
            }
        }

        private void CheckBoxUpdateSilent_Click(object sender, RoutedEventArgs e)
        {
            if((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Updater.Silent = true;
            }
            else
            {
                builder.Config.Updater.Silent = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxUpdateSilent_Click");
            }
        }

        private void RadioButtonStableUpdateChannel_Click(object sender, RoutedEventArgs e)
        {
            if((sender as RadioButton).IsChecked.Value)
            {
                builder.Config.Updater.VersionQueryUrl = null;
                ButtonSearchUpdate.IsEnabled = true;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RadioButtonStableUpdateChannel_Click");
            }
        }

        private void RadioButtonBetaUpdateChannel_Click(object sender, RoutedEventArgs e)
        {
            if((sender as RadioButton).IsChecked.Value)
            {
                builder.Config.Updater.VersionQueryUrl = BetaVersionQueryURL;
                ButtonSearchUpdate.IsEnabled = true;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RadioButtonBetaUpdateChannel_Click");
            }
        }

        /// <summary>
        /// Config folder links
        /// </summary>
        private void HyperlinkOpenConfigFile_Click(object sender, RoutedEventArgs e)
        {
            var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "config.yaml");
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        private void HyperlinkOpenConfigFile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "config.yaml");
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        private void HyperlinkOpenLogFile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode", "service.log");
            new Process
            {
                StartInfo = new ProcessStartInfo(filepath)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        private void HyperlinkOpenAppDataFolder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var folderpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
            Process.Start("explorer.exe", folderpath);
        }
    }
}
