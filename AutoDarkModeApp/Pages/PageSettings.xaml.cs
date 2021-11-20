using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeConfig;
using AutoDarkModeSvc.Communication;
using AdmProperties = AutoDarkModeConfig.Properties;
using AutoDarkModeApp.Properties;
using AutoDarkModeApp.Handlers;
using Windows.System.Power;
using System.IO;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        readonly string curLanguage = Settings.Default.Language;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly bool init = true;
        readonly Updater updater = new();
        private readonly string BetaVersionQueryURL = @"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml";
        private delegate void DispatcherDelegate();
        private const int fakeResponsiveUIDelay = 800;

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
            CheckBoxEnergySaverMitigation.ToolTip = AdmProperties.Resources.cbSettingsEnergySaverMitigationInfo;
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
                TextBlockUpdateInfo.Text = $"{AdmProperties.Resources.UpdatesTextBlockLastChecked}: {AdmProperties.Resources.UpdatesTextBlockLastCheckedNever}";
            }
            else
            {
                TextBlockUpdateInfo.Text = $"{AdmProperties.Resources.UpdatesTextBlockLastChecked}: {builder.UpdaterData.LastCheck}";

            }
            CheckBoxEnableUpdater.IsChecked = builder.Config.Updater.Enabled;
            UpdatesStackPanelOptions.IsEnabled = builder.Config.Updater.Enabled;
            switch (builder.Config.Updater.DaysBetweenUpdateCheck)
            {
                case 1:
                    ComboBoxDaysBetweenUpdateCheck.SelectedItem = ComboBoxDaysBetweenUpdateCheck1Day;
                    break;
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
            if (String.IsNullOrEmpty(builder.Config.Updater.VersionQueryUrl))
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
            CheckBoxUpdateOnStart.IsChecked = builder.Config.Updater.CheckOnStart;

            //autostart
            _ = GetAutostartInfo();
        }

        private void SetAutostartDetailsVisibility(bool visible)
        {
            if (visible)
            {
                ProgressAutostartDetails.IsActive = false;
                ProgressAutostartDetails.Visibility = Visibility.Collapsed;
                GridAutostartDetails.Visibility = Visibility.Visible;
            }
            else
            {
                ProgressAutostartDetails.IsActive = true;
                ProgressAutostartDetails.Visibility = Visibility.Visible;
                GridAutostartDetails.Visibility = Visibility.Collapsed;
            }
        }

        private async Task GetAutostartInfo(bool noToggle = false, bool toggleVisibility = true)
        {
            if (toggleVisibility) SetAutostartDetailsVisibility(false);
            try
            {
                AutostartDisabledMessage.Visibility = Visibility.Collapsed;
                CheckBoxLogonTask.IsEnabled = true;
                ToggleAutostart.IsEnabled = true;

                ApiResponse autostartResponse = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetAutostartState));
                if (autostartResponse.StatusCode == StatusCode.Err)
                {
                    ErrorMessageBoxes.ShowErrorMessageFromApi(autostartResponse, new AutoStartStatusGetException(), Window.GetWindow(this));
                }
                else if (autostartResponse.StatusCode == StatusCode.AutostartRegistryEntry)
                {
                    if (autostartResponse.Message == "Enabled")
                    {
                        if (!noToggle) ToggleAutostart.IsOn = true;
                        TextBlockAutostartMode.Text = "Registry key";
                        TextBlockAutostartPath.Text = autostartResponse.Details;
                    }
                    else
                    {
                        AutostartDisabledMessage.Visibility = Visibility.Visible;
                        ToggleAutostart.IsEnabled = false;
                        ToggleAutostart.IsOn = false;
                    }

                }
                else if (autostartResponse.StatusCode == StatusCode.AutostartTask)
                {
                    if (!noToggle) ToggleAutostart.IsOn = true;
                    TextBlockAutostartMode.Text = "Task";
                    TextBlockAutostartPath.Text = autostartResponse.Details;
                }
                else
                {
                    CheckBoxLogonTask.IsEnabled = false;

                    if (autostartResponse.StatusCode == StatusCode.Disabled)
                    {
                        if (!noToggle) ToggleAutostart.IsOn = false;
                        TextBlockAutostartMode.Text = "Disabled";
                        TextBlockAutostartPath.Text = "None";
                    }
                }
            }
            catch (Exception)
            {
                CheckBoxLogonTask.IsEnabled = false;
                ToggleAutostart.IsEnabled = false;
                ToggleAutostart.IsOn = false;
                TextBlockAutostartMode.Text = "Not found";
                TextBlockAutostartPath.Text = "None";
            }
            if (toggleVisibility) SetAutostartDetailsVisibility(true);
        }

        private void ComboBoxLanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                string selectedLanguage = ComboBoxLanguageSelection.SelectedValue.ToString();
                if (selectedLanguage != curLanguage)
                {
                    SetLanguage(selectedLanguage);
                    Translator.Text = AdmProperties.Resources.lblTranslator;
                    TextBlockLanguageRestart.Text = AdmProperties.Resources.restartNeeded;
                    TextBlockLanguageRestart.Visibility = Visibility.Visible;
                    ButtonRestart.Content = AdmProperties.Resources.restart;
                    ButtonRestart.Visibility = Visibility.Visible;
                    Settings.Default.LanguageChanged = true;
                    builder.Config.Tunable.UICulture = selectedLanguage;
                    try
                    {
                        builder.Save();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex, "comboboxlanguageselection_builder_save");
                    }
                }
                else
                {
                    SetLanguage(selectedLanguage);
                    TextBlockLanguageRestart.Visibility = Visibility.Collapsed;
                    ButtonRestart.Visibility = Visibility.Collapsed;
                    Translator.Text = AdmProperties.Resources.lblTranslator;
                    Settings.Default.LanguageChanged = false;
                }
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
                MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
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
            if (CheckBoxColourFilter.IsChecked.Value)
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
            _ = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.Switch);
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = AdmProperties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
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
                StackPanelBatterySlider.Visibility = Visibility.Visible;
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
            SetAutostartDetailsVisibility(false);
            ApiResponse result = new() { StatusCode = StatusCode.Err };
            try
            {
                builder.Config.Tunable.UseLogonTask = CheckBoxLogonTask.IsChecked ?? false;
                builder.Save();

                if (builder.Config.AutoThemeSwitchingEnabled)
                {
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                    _ = GetAutostartInfo(toggleVisibility: false);
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        CheckBoxLogonTask.IsChecked = !CheckBoxLogonTask.IsChecked;
                        builder.Config.Tunable.UseLogonTask = CheckBoxLogonTask.IsChecked ?? false;
                        builder.Save();
                        throw new AddAutoStartException($"error while processing CheckBoxLogonTask", "AutoDarkModeSvc.MessageParser.AddAutostart");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(result, ex, Window.GetWindow(this));
            }
            await Task.Delay(fakeResponsiveUIDelay);
            SetAutostartDetailsVisibility(true);
        }

        private void CheckBoxHideTrayIcon_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                MsgBox confirm = new(AdmProperties.Resources.SettingsPageTrayDisableMessageBoxContent, AdmProperties.Resources.SettingsPageTrayDisableMessageBoxHeader, "info", "yesno")
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
                _ = MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
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
            TextBlockUpdateInfo.Text = AdmProperties.Resources.msgSearchUpd;//searching for update...
            updater.CheckNewVersion();
            if (updater.UpdateAvailable())
            {
                TextBlockUpdateInfo.Text = AdmProperties.Resources.msgUpdateAvail;//a new update is available!
            }
            else
            {
                TextBlockUpdateInfo.Text = AdmProperties.Resources.msgNoUpd;//no new updates are available.
                ButtonSearchUpdate.IsEnabled = true;
            }
        }

        private void CheckBoxEnableUpdater_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Updater.Enabled = true;
                UpdatesStackPanelOptions.IsEnabled = true;
            }
            else
            {
                builder.Config.Updater.Enabled = false;
                UpdatesStackPanelOptions.IsEnabled = false;
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
                switch ((sender as ComboBox).SelectedIndex)
                {
                    case 0:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 1;
                        break;
                    case 1:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 3;
                        break;
                    case 2:
                        builder.Config.Updater.DaysBetweenUpdateCheck = 7;
                        break;
                    case 3:
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
            if ((sender as CheckBox).IsChecked.Value)
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
            if ((sender as CheckBox).IsChecked.Value)
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

        private async void RadioButtonStableUpdateChannel_Click(object sender, RoutedEventArgs e)
        {
            bool offerDowngrade = false;
            if ((sender as RadioButton).IsChecked.Value)
            {
                if (builder.Config.Updater.VersionQueryUrl != null)
                {
                    offerDowngrade = true;
                }
                builder.Config.Updater.VersionQueryUrl = null;
                ButtonSearchUpdate.IsEnabled = true;
            }
            try
            {
                builder.Save();
                if (offerDowngrade)
                {
                    _ = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForUpdate));
                    ApiResponse response = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.CheckForDowngradeNotify));
                    if (response.StatusCode == StatusCode.Downgrade)
                    {
                        TextBlockUpdateInfo.Text = "A downgrade is available";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RadioButtonStableUpdateChannel_Click");
            }

        }

        private void RadioButtonBetaUpdateChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton).IsChecked.Value)
            {
                builder.Config.Updater.VersionQueryUrl = BetaVersionQueryURL;
                builder.Config.Updater.CheckOnStart = true;
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
            CheckBoxUpdateOnStart.IsChecked = true;
        }

        /// <summary>
        /// Config folder links
        /// </summary>
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

        private void HyperlinkOpenConfigFile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                HyperlinkOpenConfigFile_PreviewMouseDown(this, null);
            }
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

        private void HyperlinkOpenLogFile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                HyperlinkOpenLogFile_PreviewMouseDown(this, null);
            }
        }

        private void HyperlinkOpenAppDataFolder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var folderpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
            Process.Start("explorer.exe", folderpath);
        }

        private void HyperlinkOpenAppDataFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                HyperlinkOpenAppDataFolder_PreviewMouseDown(this, null);
            }
        }

        private void CheckBoxUpdateOnStart_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Updater.CheckOnStart = true;
            }
            else
            {
                builder.Config.Updater.CheckOnStart = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxUpdateAtStart_Click");
            }
        }

        private async void ToggleAutostart_Toggled(object sender, RoutedEventArgs e)
        {
            ApiResponse result = new()
            {
                StatusCode = StatusCode.Err,
                Message = "error setting autostart entry"
            };
            if (!(sender as ModernWpf.Controls.ToggleSwitch).IsOn)
            {
                SetAutostartDetailsVisibility(false);
                try
                {
                    builder.Config.Autostart.Validate = true;
                    builder.Save();
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.AddAutostart));
                    await GetAutostartInfo(true, toggleVisibility: false);
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        throw new AddAutoStartException($"Could not add Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                    }
                }
                catch (Exception ex)
                {
                    ToggleAutostart.IsOn = false;
                    ErrorMessageBoxes.ShowErrorMessageFromApi(result, ex, Window.GetWindow(this));
                }
            }
            else
            {
                MsgBox confirm = new("Auto Dark Mode will no longer start with Windows and switch your theme when you log in." +
                                     "\nDo you really want to do this? ", "Disable Autostart", "info", "yesno")
                {
                    Owner = Window.GetWindow(this)
                };
                bool dialogResult = confirm.ShowDialog() ?? false;
                if (!dialogResult)
                {
                    (sender as ModernWpf.Controls.ToggleSwitch).IsOn = true;
                    return;
                }
                SetAutostartDetailsVisibility(false);
                try
                {
                    builder.Config.Autostart.Validate = false;
                    builder.Save();
                    result = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RemoveAutostart));
                    await GetAutostartInfo(true, toggleVisibility: false);
                    (sender as ModernWpf.Controls.ToggleSwitch).IsOn = false;
                    if (result.StatusCode != StatusCode.Ok)
                    {
                        throw new AddAutoStartException($"Could not remove Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessageBoxes.ShowErrorMessageFromApi(result, ex, Window.GetWindow(this));
                    ToggleAutostart.IsOn = true;
                }
            }
            await Task.Delay(fakeResponsiveUIDelay);
            SetAutostartDetailsVisibility(true);
        }

        private void ButtonAutostartValidate_Click(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(new DispatcherDelegate(ValidateAutostart));
        }

        private async void ValidateAutostart()
        {
            SetAutostartDetailsVisibility(false);
            ApiResponse response = new();
            try
            {
                response = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync($"{Command.ValidateAutostart} true", 2));
                if (response.StatusCode == StatusCode.Err)
                {
                    throw new AddAutoStartException();
                }
                await GetAutostartInfo(toggleVisibility: false);
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(response, ex, Window.GetWindow(this));
            }
            await Task.Delay(fakeResponsiveUIDelay);
            SetAutostartDetailsVisibility(true);
        }
    }
}
