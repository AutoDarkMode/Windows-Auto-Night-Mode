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
using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Handlers;
using Windows.System.Power;

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

        public PageSettings()
        {
            try
            {
                builder.Load();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "PageSettings");
            }
            InitializeComponent();
            UiHandler();
        }
        private void UiHandler()
        {
            RestartButton.Visibility = Visibility.Hidden;
            CheckBoxEnergySaverMitigation.ToolTip = Properties.Resources.cbSettingsEnergySaverMitigationInfo;
            ComboBoxLanguageSelection.SelectedValue = Settings.Default.Language.ToString();
            if(ComboBoxLanguageSelection.SelectedValue == null)
            {
                ComboBoxLanguageSelection.SelectedValue = "en";
            }

            if (!builder.Config.Tunable.DisableEnergySaverOnThemeSwitch)
            {
                SetBatterySliderVisiblity(Visibility.Collapsed);
            }
            else
            {
                CheckBoxEnergySaverMitigation.IsChecked = true;
            }

            if (PowerManager.BatteryStatus == BatteryStatus.NotPresent)
            {
                CheckBoxBatteryDarkMode.IsEnabled = false;
                CheckBoxEnergySaverMitigation.IsEnabled = false;
            }

            CheckBoxAlterTime.IsChecked = Settings.Default.AlterTime;
            CheckBoxLogonTask.IsChecked = builder.Config.Tunable.UseLogonTask;
            CheckBoxBackgroundUpdater.IsChecked = Settings.Default.BackgroundUpdate;
            CheckBoxBatteryDarkMode.IsChecked = builder.Config.Events.DarkThemeOnBattery;
            CheckBoxColourFilter.IsChecked = builder.Config.ColorFilterSwitch.Enabled;
            TextboxAccentColorDelay.Text = builder.Config.SystemSwitch.Component.TaskbarSwitchDelay.ToString();
            BatterySlider.Value = builder.Config.Tunable.BatterySliderDefaultValue;
        }

        private void ComboBoxLanguageSelection_DropDownClosed(object sender, System.EventArgs e)
        {
            string selectedLanguage = ComboBoxLanguageSelection.SelectedValue.ToString();
            if (selectedLanguage != curLanguage)
            {
                SetLanguage(selectedLanguage);
                Translator.Text = Properties.Resources.lblTranslator;
                RestartText.Text = Properties.Resources.restartNeeded;
                RestartButton.Content = Properties.Resources.restart;
                RestartButton.Visibility = Visibility.Visible;
                Settings.Default.LanguageChanged = true;
            }
            else
            {
                SetLanguage(selectedLanguage);
                RestartText.Text = null;
                RestartButton.Visibility = Visibility.Hidden;
                Translator.Text = Properties.Resources.lblTranslator;
                Settings.Default.LanguageChanged = false;
            }
        }

        private void SetLanguage(string lang)
        {
            Settings.Default.Language = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
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

        private void CheckBoxBackgroundUpdater_Click(object sender, RoutedEventArgs e)
        {

            if (CheckBoxBackgroundUpdater.IsChecked.Value)
            {
                TaskSchdHandler.CreateAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = true;
            }
            else
            {
                TaskSchdHandler.RemoveAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = false;
            }
        }

        private void CheckBoxBatteryDarkMode_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxBatteryDarkMode.IsChecked.Value)
            {
                builder.Config.Events.DarkThemeOnBattery = true;
            }
            else
            {
                builder.Config.Events.DarkThemeOnBattery = false;
            }
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "CheckBoxBatteryDarkMode_Click");
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
            await messagingClient.SendMessageAsync(Command.Switch);
        }

        private void TextboxAccentColorDelay_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextboxAccentColorDelay.Dispatcher.BeginInvoke(new Action(() =>
            {
                TextboxAccentColorDelay.SelectAll();
            }));
        }
        private void TextboxAccentColorDelay_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextboxAccentColorDelay_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TextboxAccentColorDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextboxAccentColorDelay.Text != "")
            {
                builder.Config.SystemSwitch.Component.TaskbarSwitchDelay = int.Parse(TextboxAccentColorDelay.Text);
                try
                {
                    builder.Save();
                }
                catch(Exception ex)
                {
                    ShowErrorMessage(ex, "TextboxAccentColorDelay_TextChanged");
                }
            }
        }

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void ShowErrorMessage(Exception ex, string location)
        {
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
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
                SetBatterySliderVisiblity(Visibility.Visible);
            }
            else
            {
                builder.Config.Tunable.DisableEnergySaverOnThemeSwitch = false;
                SetBatterySliderVisiblity(Visibility.Collapsed);
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

        private void SetBatterySliderVisiblity(Visibility visibility)
        {
            BatterySlider.Visibility = visibility;
            BatterySliderLabel.Visibility = visibility;
            BatterySliderText.Visibility = visibility;
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
                    if (result != Response.Ok)
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
    }
}
