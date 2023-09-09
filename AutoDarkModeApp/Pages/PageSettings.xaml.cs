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
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using AdmProperties = AutoDarkModeLib.Properties;
using AutoDarkModeApp.Properties;
using AutoDarkModeApp.Handlers;
using Windows.System.Power;
using System.IO;
using System.Threading.Tasks;
using System.Management;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        readonly string curLanguage = Settings.Default.Language;
        string selectedLanguage = Settings.Default.Language;
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly bool init = true;
        readonly Updater updater = new();
        private readonly string BetaVersionQueryURL = @"https://raw.githubusercontent.com/AutoDarkMode/AutoDarkModeVersion/master/version-beta.yaml";
        private delegate void DispatcherDelegate();
        private const int fakeResponsiveUIDelay = 800;
        private readonly ManagementEventWatcher autostartWatcher;

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

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                FontIconLinkConfig.FontFamily = new("Segoe Fluent Icons");
                FontIconLinkConfigFolder.FontFamily = new("Segoe Fluent Icons");
            }

            try
            {
                string sidString = SID.ToString();
                string queryString = $"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = " +
                    $"'{sidString}\\\\" +
                    @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run' AND ValueName='AutoDarkMode'";
                WqlEventQuery query = new WqlEventQuery(queryString);
                autostartWatcher = new ManagementEventWatcher(query);
                autostartWatcher.EventArrived += new EventArrivedEventHandler(HandleAutostartEnabledEvent);
                autostartWatcher.Start();
            }
            catch (ManagementException manEx)
            {
                if (manEx.ErrorCode != ManagementStatus.NotFound)
                {
                    ShowErrorMessage(manEx, "(non-critical) Settings Constructor Regkey Watcher");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "(non-critical) Settings Constructor Regkey Watcher");
            }
            init = false;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                autostartWatcher.Stop();
                autostartWatcher.Dispose();
            }
            catch { }            
        }

        private static SecurityIdentifier SID
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User;
            }
        }

        private void UiHandler()
        {
            //hide elements which aren't compatible with this device
            if (PowerManager.BatteryStatus == BatteryStatus.NotPresent || Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                CheckBoxEnergySaverMitigation.Visibility = Visibility.Collapsed;
                CardEnergySaverMitigation.Visibility = Visibility.Collapsed;
            }

            //language ui
            DockPanelLanguageRestart.Visibility = Visibility.Collapsed;
            ComboBoxLanguageSelection.SelectedValue = Settings.Default.Language.ToString().Replace("-", "_");
            if (ComboBoxLanguageSelection.SelectedValue == null)
            {
                ComboBoxLanguageSelection.SelectedValue = "en";
            }

            // checkboxes
            CheckBoxAlterTime.IsChecked = Settings.Default.AlterTime;
            CheckBoxLogonTask.IsChecked = builder.Config.Tunable.UseLogonTask;
            CheckBoxHideTrayIcon.IsChecked = !builder.Config.Tunable.ShowTrayIcon;
            CheckBoxWin10AllowLockscreenSwitch.IsChecked = builder.Config.Events.Win10AllowLockscreenSwitch;
            CheckBoxAlwaysDwmRefresh.IsChecked = builder.Config.Tunable.AlwaysFullDwmRefresh;

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                CardWin10AllowLockscreenSwitch.Visibility = Visibility.Collapsed;
            }
            else
            {
                CardWin10AllowLockscreenSwitch.Visibility = Visibility.Visible;
            }

            CheckBoxDebugMode.IsChecked = builder.Config.Tunable.Debug;
            CheckBoxTraceMode.IsChecked = builder.Config.Tunable.Trace;
            if (!builder.Config.Tunable.Debug)
            {
                CheckBoxTraceMode.Visibility = Visibility.Collapsed;
                CardTraceMode.Visibility = Visibility.Collapsed;
            }

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
            ToggleSwitchEnableUpdater.IsOn = builder.Config.Updater.Enabled;
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
                ApiResponse autostartResponse = ApiResponse.FromString(await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GetAutostartState));
                if (autostartResponse.StatusCode == StatusCode.Err)
                {
                    ErrorMessageBoxes.ShowErrorMessageFromApi(autostartResponse, new AutoStartStatusGetException(), Window.GetWindow(this));
                }
                else if (autostartResponse.StatusCode == StatusCode.AutostartRegistryEntry)
                {
                    if (autostartResponse.Message == "Enabled")
                    {
                        ButtonAutostartValidate.IsEnabled = true;
                        CheckBoxLogonTask.IsEnabled = true;
                        StackPanelAutostart.IsEnabled = true;
                        if (!noToggle) ToggleAutostart.IsOn = true;
                        TextBlockAutostartMode.Text = "Registry key";
                        TextBlockAutostartPath.Text = autostartResponse.Details;
                    }
                    else
                    {
                        if (!noToggle) ToggleAutostart.IsOn = false;
                        ButtonAutostartValidate.IsEnabled = false;
                        AutostartDisabledMessage.Visibility = Visibility.Visible;
                        StackPanelAutostart.IsEnabled = false;
                        CheckBoxAutoInstall.IsEnabled = false;
                    }
                }
                else if (autostartResponse.StatusCode == StatusCode.AutostartTask)
                {
                    ButtonAutostartValidate.IsEnabled = true;
                    CheckBoxLogonTask.IsEnabled = true;
                    if (!noToggle) ToggleAutostart.IsOn = true;
                    CheckBoxLogonTask.IsEnabled = true;
                    TextBlockAutostartMode.Text = "Task";
                    TextBlockAutostartPath.Text = autostartResponse.Details;
                }
                else if (autostartResponse.StatusCode == StatusCode.Disabled)
                {
                    ButtonAutostartValidate.IsEnabled = false;
                    if (!noToggle) ToggleAutostart.IsOn = false;
                    CheckBoxLogonTask.IsEnabled = false;
                    TextBlockAutostartMode.Text = "Disabled";
                    TextBlockAutostartPath.Text = "None";
                }
                else
                {
                    CheckBoxLogonTask.IsEnabled = false;
                }
            }
            catch (Exception)
            {
                CheckBoxLogonTask.IsEnabled = false;
                StackPanelAutostart.IsEnabled = false;
                TextBlockAutostartMode.Text = "Not found";
                TextBlockAutostartPath.Text = "None";
            }
            if (toggleVisibility) SetAutostartDetailsVisibility(true);
        }

        private void ComboBoxLanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!init)
            {
                selectedLanguage = ComboBoxLanguageSelection.SelectedValue.ToString().Replace("_", "-");
                if (selectedLanguage != curLanguage)
                {
                    Translator.Text = AdmProperties.Resources.ResourceManager.GetString("lblTranslator", new(selectedLanguage));
                    DockPanelLanguageRestart.Visibility = Visibility.Visible;
                    TextBlockLanguageRestart.Text = AdmProperties.Resources.ResourceManager.GetString("restartNeeded", new(selectedLanguage));
                    ButtonRestart.Content = AdmProperties.Resources.ResourceManager.GetString("restart", new(selectedLanguage));
                    Settings.Default.LanguageChanged = true;

                }
                else
                {
                    Translator.Text = AdmProperties.Resources.lblTranslator;
                    DockPanelLanguageRestart.Visibility = Visibility.Collapsed;
                    TextBlockLanguageRestart.Text = AdmProperties.Resources.restartNeeded;
                    ButtonRestart.Content = AdmProperties.Resources.restart;
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
                SetLanguage(selectedLanguage);
                builder.Config.Tunable.UICulture = selectedLanguage;
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "comboboxlanguageselection_builder_save");
                }

                MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
                Settings.Default.Save();
                Process.Start(new ProcessStartInfo(Helper.ExecutionPathApp)
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

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = AdmProperties.Resources.ErrorMessageBox + $"\n\nError ocurred in: {location} " + ex.Source + "\n\n" + ex.Message;
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

        private void CheckBoxWin10AllowLockscreenSwitch_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Events.Win10AllowLockscreenSwitch = true;
            }
            else
            {
                builder.Config.Events.Win10AllowLockscreenSwitch  = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxWin10MultiUser_Click");
            }
        }

        private void CheckBoxDebugMode_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Tunable.Debug = true;
                CheckBoxTraceMode.Visibility = Visibility.Visible;
                CardTraceMode.Visibility = Visibility.Visible;
            }
            else
            {
                builder.Config.Tunable.Debug  = false;
                builder.Config.Tunable.Trace = false;
                CheckBoxTraceMode.IsChecked = false;
                CheckBoxTraceMode.Visibility = Visibility.Collapsed;
                CardTraceMode.Visibility = Visibility.Collapsed;
            }
            try
            {
                builder.Save();
                //_ = MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxDebugMode_Click");
            }
        }

        private void CheckBoxTraceMode_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                builder.Config.Tunable.Trace = true;
            }
            else
            {
                builder.Config.Tunable.Trace = false;
            }
            try
            {
                builder.Save();
                //_ = MessageHandler.Client.SendMessageAndGetReply(Command.Restart);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxTraceMode_Click");
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

        private void ToggleSwitchEnableUpdater_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as ModernWpf.Controls.ToggleSwitch).IsOn)
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
                if (!init) builder.Save();
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
                        TextBlockUpdateInfo.Text = AdmProperties.Resources.SettingsPageDowngradeAvailable;
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
                builder.Config.Tunable.Debug = true;
                CheckBoxTraceMode.Visibility = Visibility.Visible;
                CardTraceMode.Visibility = Visibility.Visible;
                CheckBoxDebugMode.IsChecked = true;
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

        private async void HandleAutostartEnabledEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                await Dispatcher.Invoke(async () => { await GetAutostartInfo(toggleVisibility: false); });
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "autostart_monitor_event");
            }
        }

        private void CheckBoxAlwaysRefreshDwm_Click(object sender, RoutedEventArgs e)
        {
            MsgBox confirm = new(AdmProperties.Resources.SettingsPageCheckBoxAlwaysRefreshDwmExplanation, 
                AdmProperties.Resources.SettingsPageCheckBoxAlwaysRefreshDwmHeader, 
                "info", 
                "okcancel")
            {
                Owner = Window.GetWindow(this)
            };
            if (CheckBoxAlwaysDwmRefresh.IsChecked == true)
            {
                bool? result = confirm.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    builder.Config.Tunable.AlwaysFullDwmRefresh = true;
                }
                else
                {
                    CheckBoxAlwaysDwmRefresh.IsChecked = false;
                }
            }         
            else
            {
                builder.Config.Tunable.AlwaysFullDwmRefresh = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxAlwaysDwmRefresh");
            }
        }
    }
}
