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
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Windows.System.Power;
using AdmProperties = AutoDarkModeLib.Properties;
using AutoDarkModeApp.Properties;
using AutoDarkModeLib;
using System.Globalization;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;
using AutoDarkModeApp.Handlers;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using SourceChord.GridExtra;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageTime.xaml
    /// </summary>
    public partial class PageTime : Page
    {
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private bool init = true;
        private bool reload = false;

        private delegate void DispatcherDelegate();

        private int selectedPostponeMinutes = -0;

        public PageTime()
        {
            //read config file
            try
            {
                builder.Load();
                builder.LoadLocationData();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime");
            }


            StateUpdateHandler.OnConfigUpdate += HandleConfigUpdate;
            StateUpdateHandler.StartConfigWatcher();

            //initialize ui components
            InitializeComponent();

            BreakPoints bp = new()
            {
                XS_SM = 550,
                SM_MD = ResponsiveLocationGrid.BreakPoints.SM_MD,
                MD_LG= ResponsiveLocationGrid.BreakPoints.MD_LG
            };
            ResponsiveLocationGrid.BreakPoints = bp;


            NumberBoxOffsetLight.Header = $"{AdmProperties.Resources.lblLight} ({AdmProperties.Resources.Minutes})";
            NumberboxOffsetDark.Header = $"{AdmProperties.Resources.lblDark} ({AdmProperties.Resources.Minutes})";

            //enable 12 hour clock:
            if (Properties.Settings.Default.AlterTime)
            {
                TimePickerDark.Culture = CultureInfo.CreateSpecificCulture("en");
                TimePickerLight.Culture = CultureInfo.CreateSpecificCulture("en");
            }
            //enable 24 hour clock:
            else
            {
                TimePickerDark.Culture = CultureInfo.CreateSpecificCulture("de");
                TimePickerLight.Culture = CultureInfo.CreateSpecificCulture("de");
            }

            //StackPanelPostponeInfo.Visibility = Visibility.Collapsed;
            TextBlockResumeInfo.Visibility = Visibility.Collapsed;
            StateUpdateHandler.OnPostponeTimerTick += PostponeTimerEvent;
            StateUpdateHandler.StartPostponeTimer();
            PostponeTimerEvent(null, new());

            LoadSettings();

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_RC)
            {
                FontIconLocation.FontFamily = new("Segoe Fluent Icons");
                FontIconLight.FontFamily = new("Segoe Fluent Icons");
                FontIconDark.FontFamily = new("Segoe Fluent Icons");
                FontIconGeoLink.FontFamily = new("Segoe Fluent Icons");
                FontIconNightLightLink.FontFamily = new("Segoe Fluent Icons");
            }

            Window window = Application.Current.MainWindow;
            window.StateChanged += (s, e) =>
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    StateUpdateHandler.StopPostponeTimer();
                }
                else
                {
                    PostponeTimerEvent(null, new());
                    StateUpdateHandler.StartPostponeTimer();
                }
            };
        }


        private void HandleConfigUpdate(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StateUpdateHandler.StopConfigWatcher();
                init = true;
                reload = true;
                try
                {
                    builder.Load();
                }
                catch
                {

                }
                LoadSettings();
            });
        }

        /// <summary>
        /// Repeatedly retrieves the postpone state and handles UI changes if the state has changed externally
        /// </summary>
        private void PostponeTimerEvent(object sender, EventArgs e)
        {
            ApiResponse reply =
                ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.GetPostponeStatus));
            if (reply.StatusCode != StatusCode.Timeout)
            {
                if (builder.Config.AutoThemeSwitchingEnabled)
                {
                    try
                    {
                        if (reply.Message == "True")
                        {
                            bool anyNoExpiry = false;
                            bool canResume = false;
                            PostponeQueueDto dto = PostponeQueueDto.Deserialize(reply.Details);
                            List<string> itemsStringList = dto.Items.Select(i =>
                            {
                                if (i.Expiry == null) anyNoExpiry = true;
                                if (i.IsUserClearable)
                                    canResume = true;

                                i.SetCulture(Thread.CurrentThread.CurrentCulture);

                                // retrieve the value of the specified key
                                i.TranslatedReason =
                                    AdmProperties.Resources.ResourceManager.GetString("PostponeReason" + i.Reason) ??
                                    i.Reason;

                                return i.ToString();
                            }).ToList();
                            Dispatcher.Invoke(() =>
                            {
                                if (anyNoExpiry && !canResume)
                                {
                                    TextBlockResumeInfo.Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    TextBlockResumeInfo.Visibility = Visibility.Collapsed;
                                }

                                if (canResume)
                                {
                                    ButtonControlPostponeQueue.Content = AdmProperties.Resources.Resume;
                                    PostponeComboBox.IsEnabled = false;
                                }
                                else
                                {
                                    ButtonControlPostponeQueue.Content = AdmProperties.Resources.PostponeButtonDelay;
                                    PostponeComboBox.IsEnabled = true;
                                }

                                TextBlockPostponeInfo.Text = string.Join('\n', itemsStringList);
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                //StackPanelPostponeInfo.Visibility = Visibility.Collapsed;
                                ButtonControlPostponeQueue.Content = AdmProperties.Resources.PostponeButtonDelay;
                                TextBlockPostponeInfo.Text = AdmProperties.Resources.TimePagePostponeInfoNominal;
                                TextBlockResumeInfo.Visibility = Visibility.Collapsed;
                                PostponeComboBox.IsEnabled = true;
                            });
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void LoadSettings()
        {
            //read datetime from config file
            TimePickerDark.SelectedDateTime = builder.Config.Sunset;
            TimePickerLight.SelectedDateTime = builder.Config.Sunrise;

            //read offset from config file
            NumberBoxOffsetLight.Value = Convert.ToDouble(builder.Config.Location.SunriseOffsetMin);
            NumberboxOffsetDark.Value = Convert.ToDouble(builder.Config.Location.SunsetOffsetMin);

            //read coordinates from config file
            NumberBoxLat.Text = builder.Config.Location.CustomLat.ToString(CultureInfo.InvariantCulture);
            NumberBoxLon.Text = builder.Config.Location.CustomLon.ToString(CultureInfo.InvariantCulture);
            ButtonApplyCoordinates.IsEnabled = false;

            //tick correct radio button and prepare UI
            //is auto theme switch enabled?
            //auto theme switch is: disabled
            if (!builder.Config.AutoThemeSwitchingEnabled)
            {
                StackPanelModeSelection.IsEnabled = false;
                SetPanelVisibility(false, false, false, false, false);
                ToggleAutoSwitchEnabled.IsOn = false;

                if (builder.Config.Governor == Governor.NightLight) RadioButtonWindowsNightLight.IsChecked = true;
                else if (!builder.Config.Location.Enabled) RadioButtonCustomTimes.IsChecked = true;
                else if (builder.Config.Location.UseGeolocatorService) RadioButtonLocationTimes.IsChecked = true;
                else RadioButtonCoordinateTimes.IsChecked = true;

                // welcome text for disabled automatic theme switch
                userFeedback.Text = AdmProperties.Resources.welcomeText;
            }
            //auto theme switch is: enabled
            else
            {
                StackPanelModeSelection.IsEnabled = true;
                ToggleAutoSwitchEnabled.IsOn = true;
                if (builder.Config.Governor == Governor.Default)
                {
                    NumberboxOffsetDark.Minimum = -999;
                    NumberBoxOffsetLight.Minimum = -999;
                    //is custom timepicker input enabled?
                    if (!builder.Config.Location.Enabled)
                    {
                        RadioButtonCustomTimes.IsChecked = true;
                        SetPanelVisibility(true, false, false, false, true);
                        userFeedback.Text = AdmProperties.Resources.msgChangesSaved;
                        applyButton.IsEnabled = false;
                    }

                    //is location mode enabled?
                    if (builder.Config.Location.Enabled)
                    {
                        //windows location service
                        if (builder.Config.Location.UseGeolocatorService)
                        {
                            SetPanelVisibility(false, true, true, false, true);
                            ActivateLocationModeWrapper();
                            RadioButtonLocationTimes.IsChecked = true;
                        }
                        //custom geographic coordinates
                        else
                        {
                            RadioButtonCoordinateTimes.IsChecked = true;
                            SetPanelVisibility(false, true, true, true, true);
                            ActivateLocationModeWrapper();
                        }
                    }
                }
                else if (builder.Config.Governor == Governor.NightLight)
                {
                    RadioButtonWindowsNightLight.IsChecked = true;
                    NumberboxOffsetDark.Minimum = 0;
                    NumberBoxOffsetLight.Minimum = 0;
                    SetPanelVisibility(false, false, true, false, true, true);
                }
            }

            init = false;
            StateUpdateHandler.StartConfigWatcher();
        }

        /// <summary>
        /// Offset
        /// for sunrise and sunset hours
        /// </summary>
        //apply offset
        private void OffsetButton_Click(object sender, RoutedEventArgs e)
        {
            int offsetDark;
            int offsetLight;

            //get values from TextBox
            try
            {
                offsetDark = Convert.ToInt32(NumberboxOffsetDark.Value);
                offsetLight = Convert.ToInt32(NumberBoxOffsetLight.Value);
            }
            catch
            {
                userFeedback.Text = AdmProperties.Resources.errorNumberInput;
                return;
            }

            //send the values / offset to Svc
            try
            {
                builder.Config.Location.SunriseOffsetMin = offsetLight;
                builder.Config.Location.SunsetOffsetMin = offsetDark;
            }
            catch
            {
                userFeedback.Text = "Error while sending offset digits to Svc";
                return;
            }

            OffsetButton.IsEnabled = false;
            UpdateSuntimes();
            Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            try
            {
                if (!init) builder.Save();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime OffsetButton_Click");
            }
        }


        private void SetPanelVisibility(bool timepicker = false, bool location = false, bool offset = false,
            bool coordinates = false, bool postpone = false, bool nightLight = false)
            {
            if (timepicker) GridTimePicker.Visibility = Visibility.Visible;
            else GridTimePicker.Visibility = Visibility.Collapsed;

            if (location) GridLocationTimeInfo.Visibility = Visibility.Visible;
            else GridLocationTimeInfo.Visibility = Visibility.Collapsed;

            if (offset) GridOffset.Visibility = Visibility.Visible;
            else GridOffset.Visibility = Visibility.Collapsed;

            if (coordinates) GridCoordinates.Visibility = Visibility.Visible;
            else GridCoordinates.Visibility = Visibility.Collapsed;

            if (postpone) StackPanelPostponeInfo.Visibility = Visibility.Visible;
            else StackPanelPostponeInfo.Visibility = Visibility.Collapsed;

            if (nightLight)
            {
                GridNightLight.Visibility = Visibility.Visible;
                TextBlockNightLightHeader.Visibility = Visibility.Visible;
            }
            else
            {
                GridNightLight.Visibility = Visibility.Collapsed;
                TextBlockNightLightHeader.Visibility = Visibility.Collapsed;
            }

        }

        //apply theme
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int darkStartHours;
            int darkStartMinutes;
            int lightStartHours;
            int lightStartMinutes;

            //get values from TextBox
            try
            {
                darkStartHours = TimePickerDark.SelectedDateTime.Value.Hour;
                darkStartMinutes = TimePickerDark.SelectedDateTime.Value.Minute;
                lightStartHours = TimePickerLight.SelectedDateTime.Value.Hour;
                lightStartMinutes = TimePickerLight.SelectedDateTime.Value.Minute;
            }
            catch
            {
                userFeedback.Text = AdmProperties.Resources.errorNumberInput;
                return;
            }

            //check values from timepicker
            //currently nothing to check

            //display edited hour values for the user
            //disabled because we don't check and edit anything
            //TimePickerDark.SelectedDateTime = new DateTime(2000, 08, 22, darkStart, darkStartMinutes, 0);
            //TimePickerLight.SelectedDateTime = new DateTime(2000, 08, 22, lightStart, lightStartMinutes, 0);

            //Apply Theme
            builder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStartHours,
                darkStartMinutes, 0);
            builder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStartHours,
                lightStartMinutes, 0);

            try
            {
                if (!init) builder.Save();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime");
            }

            Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));

            //ui
            //applyButton.IsEnabled = false;
        }

        private async void ApplyTheme()
        {
            //show warning for notebook on battery with enabled battery saver
            if (!builder.Config.Tunable.DisableEnergySaverOnThemeSwitch &&
                PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                userFeedback.Text = AdmProperties.Resources.msgChangesSaved + "\n\n" +
                                    AdmProperties.Resources.msgBatterySaver;
                applyButton.IsEnabled = true;
            }
            else
            {
                userFeedback.Text = AdmProperties.Resources.msgChangesSaved; //changes were saved!
            }

            try
            {
                string result = await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.RequestSwitch, 15);
                if (result != StatusCode.Ok)
                {
                    throw new SwitchThemeException(result, "PageTime");
                }
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme($"Error while applying theme: ", ex.ToString());
            }
        }

        //if something went wrong while applying the settings :(
        private void ErrorWhileApplyingTheme(string erroDescription, string exception)
        {
            userFeedback.Text = AdmProperties.Resources.msgErrorOcc;
            string error =
                string.Format(AdmProperties.Resources.ErrorMessageBox,
                    AdmProperties.Resources.cbSettingsMultiUserImprovements) + "\n\n" + erroDescription + "\n\n" +
                exception;
            MsgBox msg = new(error, AdmProperties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            _ = msg.ShowDialog();
            bool? result = msg.DialogResult;
            if (result == true)
            {
                ProcessHandler.StartProcessByProcessInfo(
                    "https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
            }
        }

        public async void ActivateLocationModeWrapper()
        {
            if (!reload)
            {
                await ActivateLocationMode();
            }

            reload = false;
        }

        /// <summary>
        /// Location based times & Windows Position Service
        /// </summary>
        // set starttime based on user location
        public async Task ActivateLocationMode()
        {
            //ui
            locationBlock.Text = AdmProperties.Resources.msgSearchLoc; //Searching your location...
            userFeedback.Text = AdmProperties.Resources.msgSearchLoc;

            await LoadGeolocationData();
            UpdateSuntimes();

            // ui controls
            userFeedback.Text = AdmProperties.Resources.msgChangesSaved;

            return;
        }

        private async Task LoadGeolocationData()
        {
            int maxTries = 5;
            for (int i = 0; i < maxTries; i++)
            {
                ApiResponse result =
                    ApiResponse.FromString(
                        await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.GeolocatorIsUpdating));
                if (result.StatusCode == StatusCode.Ok)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            try
            {
                builder.LoadLocationData();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime");
            }

            try
            {
                ApiResponse result =
                    ApiResponse.FromString(
                        await MessageHandler.Client.SendMessageAndGetReplyAsync(Command.LocationAccess));
                if (builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.NoLocAccess)
                {
                    NoLocationAccess();
                    return;
                }
                else if (builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.Ok)
                {
                    locationBlock.Text = AdmProperties.Resources.lblCity + ": " + await LocationHandler.GetCityName();
                }
                else if (!builder.Config.Location.UseGeolocatorService)
                {
                    locationBlock.Text =
                        $"{AdmProperties.Resources.lblPosition}: Lat {Math.Round(builder.LocationData.Lat, 3)} / Lon {Math.Round(builder.LocationData.Lon, 3)}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime");
                return;
            }
        }

        private async void NoLocationAccess()
        {
            locationBlock.Text = AdmProperties.Resources.msgLocPerm; //The App needs permission to location
            userFeedback.Text = AdmProperties.Resources.msgLocPerm;
            locationBlock.Visibility = Visibility.Visible;
            TextBlockDarkTime.Text = null;
            TextBlockLightTime.Text = null;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void DisableLocationMode()
        {
            SetPanelVisibility(true, false, false, false, true);

            builder.Config.Location.Enabled = false;
            userFeedback.Text = AdmProperties.Resources.msgClickApply; //Click on apply to save changes
        }

        private void UpdateSuntimes()
        {
            LocationHandler.GetSunTimesWithOffset(builder, out DateTime SunriseWithOffset,
                out DateTime SunsetWithOffset);
            if (Settings.Default.AlterTime)
            {
                TextBlockLightTime.Text = AdmProperties.Resources.lblLight + ": " +
                                          SunriseWithOffset.ToString("hh:mm tt",
                                              CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = AdmProperties.Resources.lblDark + ": " +
                                         SunsetWithOffset.ToString("hh:mm tt",
                                             CultureInfo.InvariantCulture); //textblock2
            }
            else
            {
                TextBlockLightTime.Text = AdmProperties.Resources.lblLight + ": " +
                                          SunriseWithOffset.ToString("HH:mm",
                                              CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = AdmProperties.Resources.lblDark + ": " +
                                         SunsetWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture); //textblock2
            }

            DateTime nextUpdate = builder.LocationData.LastUpdate.Add(builder.Config.Location.PollingCooldownTimeSpan);
            if (Settings.Default.AlterTime)
                LocationNextUpdateDate.Text = nextUpdate.ToString(CultureInfo.CreateSpecificCulture("en"));
            else LocationNextUpdateDate.Text = nextUpdate.ToString(CultureInfo.CreateSpecificCulture("de"));
        }

        /// <summary>
        /// Geographic Coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonApplyCoordinates_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.Governor = Governor.Default;
            try
            {
                builder.Config.Location.CustomLat = double.Parse(NumberBoxLat.Text, CultureInfo.InvariantCulture);
                builder.Config.Location.CustomLon = double.Parse(NumberBoxLon.Text, CultureInfo.InvariantCulture);
            }
            catch
            {
                userFeedback.Text = AdmProperties.Resources.errorNumberInput;
                return;
            }

            builder.Config.Location.Enabled = true;
            builder.Config.Location.UseGeolocatorService = false;
            try
            {
                if (!init) builder.Save();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime");
            }

            ButtonApplyCoordinates.IsEnabled = false;
            await ActivateLocationMode();
            await Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            SetPanelVisibility(false, true, true, true, true);
        }


        /// <summary>
        /// radio buttons
        /// </summary>
        private void ToggleAutoSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (init) return;
            try
            {
                if (ToggleAutoSwitchEnabled.IsOn)
                {
                    builder.Config.AutoThemeSwitchingEnabled = true;
                    builder.Save();
                    init = true;
                    LoadSettings();
                }
                else
                {
                    builder.Config.AutoThemeSwitchingEnabled = false;
                    builder.Save();
                    SetPanelVisibility(false, false, false, false, true);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "ToggleAutoSwitch");
            }

        }

        private void RadioButtonCustomTimes_Click(object sender, RoutedEventArgs e)
        {
            NumberboxOffsetDark.Minimum = -999;
            NumberBoxOffsetLight.Minimum = -999;
            DisableLocationMode();
            builder.Config.Governor = Governor.Default;
            //applyButton.IsEnabled = true;
            try
            {
                if (!init) builder.Save();
                Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime RadioButtonCustomTimes_Click");
            }
        }

        private async void RadioButtonLocationTimes_Click(object sender, RoutedEventArgs e)
        {
            NumberboxOffsetDark.Minimum = -999;
            NumberBoxOffsetLight.Minimum = -999;
            builder.Config.Location.Enabled = true;
            builder.Config.Location.UseGeolocatorService = true;
            builder.Config.Governor = Governor.Default;
            try
            {
                if (!init) builder.Save();
                SetPanelVisibility(false, true, true, false, true);
                await ActivateLocationMode();
                await Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime RadioButtonLocationTimes_Click");
            }
        }

        private void RadioButtonCoordinateTimes_Click(object sender, RoutedEventArgs e)
        {
            NumberboxOffsetDark.Minimum = -999;
            NumberBoxOffsetLight.Minimum = -999;
            if (builder.Config.Location.CustomLat != 0 & builder.Config.Location.CustomLon != 0)
            {
                SetPanelVisibility(false, false, true, true, true);
                ButtonApplyCoordinates_Click(this, null);
            }
            else
            {
                SetPanelVisibility(false, false, false, true, true);
            }
        }

        /// <summary>
        /// Just UI stuff
        /// Events of controls
        /// </summary>
        private void TimePicker_SelectedDateTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if (!init)
            {
                //applyButton.IsEnabled = true;
                ApplyButton_Click(this, null);
            }
        }

        private void TextBlockHelpWiki_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessHandler.StartProcessByProcessInfo(
                "https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Troubleshooting");
        }

        private void TextBlockHelpWiki_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockHelpWiki_MouseDown(this, null);
            }
        }

        //numbox event handler
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TextBox_BlockCopyPaste_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void TexttBox_SelectAll_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Dispatcher.BeginInvoke(new System.Action(() => { textBox.SelectAll(); }));
        }

        private void NumberBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void NumberBox_ValueChanged(ModernWpf.Controls.NumberBox sender,
            ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if (sender.Tag.Equals("offset"))
                {
                    if (OffsetButton != null)
                    {
                        OffsetButton.IsEnabled = true;
                    }

                    userFeedback.Text = AdmProperties.Resources.TimeTextBlockClickOnSetMessage;
                }
                else if (sender.Tag.Equals("coordinates"))
                {
                    if (ButtonApplyCoordinates != null)
                    {
                        ButtonApplyCoordinates.IsEnabled = true;
                    }

                    userFeedback.Text = AdmProperties.Resources.msgClickApply; //Click on apply to save changes
                }
            }
        }

        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!init)
            {
                if (sender is ModernWpf.Controls.NumberBox nb && nb.Tag.Equals("offset"))
                {
                    if (OffsetButton != null)
                    {
                        OffsetButton.IsEnabled = true;
                    }

                    userFeedback.Text = AdmProperties.Resources.TimeTextBlockClickOnSetMessage;
                }
                else if (sender is TextBox tb && tb.Tag.Equals("coordinates"))
                {
                    if (ButtonApplyCoordinates != null)
                    {
                        ButtonApplyCoordinates.IsEnabled = true;
                    }

                    userFeedback.Text = AdmProperties.Resources.msgClickApply; //Click on apply to save changes
                }
            }
        }

        private void TextBlockOpenLatLongWebsite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessHandler.StartProcessByProcessInfo("https://www.latlong.net/");
        }

        private void TextBlockOpenLatLongWebsite_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockOpenLatLongWebsite_MouseDown(this, null);
            }
        }

        private void NumberBox_Validate(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string validated = tb.Text.Replace(",", ".");
                bool isNegative = false;
                if (validated.StartsWith("-")) isNegative = true;
                validated = Regex.Replace(validated, @"[^\d.]", "");

                if (validated.Contains("."))
                {
                    string[] split = validated.Split(".");
                    string join = string.Join("", split[1..]);
                    join = Regex.Replace(join, @"[^\d]", "");
                    validated = $"{split[0]}.{join}";
                    validated = validated.TrimEnd('0').TrimEnd('.');
                }

                if (validated.StartsWith('0'))
                {
                    validated = validated.TrimStart('0');
                    if (validated.StartsWith('.')) validated = "0" + validated;
                }

                if (validated.Length == 0)
                {
                    validated = "0";
                }

                if (isNegative)
                {
                    validated = "-" + validated;
                }

                tb.Text = validated;

                _ = double.TryParse(NumberBoxLat.Text, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double latParsed);
                _ = double.TryParse(NumberBoxLon.Text, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double lonParsed);
                if (latParsed > 90) NumberBoxLat.Text = "90";
                else if (latParsed < -90) NumberBoxLat.Text = "-90";
                if (lonParsed > 180) NumberBoxLon.Text = "180";
                else if (lonParsed < -180) NumberBoxLon.Text = "-180";
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectionStart = tb.Text.Length;
            tb.SelectionLength = 0;
        }

        private void NumberBoxLatLon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ButtonApplyCoordinates != null)
            {
                ButtonApplyCoordinates.IsEnabled = true;
            }

            userFeedback.Text = AdmProperties.Resources.msgClickApply; //Click on apply to save changes
        }

        private void ButtonControlPostponeQueue_Click(object sender, RoutedEventArgs e)
        {
            bool isDelayed = false;
            if ((string)ButtonControlPostponeQueue.Content == AdmProperties.Resources.Resume)
            {
                ButtonControlPostponeQueue.Content = AdmProperties.Resources.PostponeButtonDelay;
                PostponeComboBox.IsEnabled = true;
                isDelayed = true;
            }
            else
            {
                ButtonControlPostponeQueue.Content = AdmProperties.Resources.Resume;
                PostponeComboBox.IsEnabled = false;
            }

            if (selectedPostponeMinutes != 0 && !isDelayed)
            {
                MessageHandler.Client.SendMessageAndGetReply($"{Command.DelayBy} {selectedPostponeMinutes}");
            }
            else if (selectedPostponeMinutes == 0 && !isDelayed)
            {
                MessageHandler.Client.SendMessageAndGetReply(Command.ToggleSkipNext);
                if (isDelayed) MessageHandler.Client.SendMessageAndGetReply(Command.RequestSwitch);
            }
            else
            {
                MessageHandler.Client.SendMessageAndGetReply(Command.ClearPostponeQueue);
                MessageHandler.Client.SendMessageAndGetReply(Command.RequestSwitch);
            }

            PostponeTimerEvent(null, new());
        }

        private void RadioButtonWindowsNightLight_Click(object sender, RoutedEventArgs e)
        {
            SetPanelVisibility(false, false, true, false, true);
            builder.Config.Governor = Governor.NightLight;
            builder.Config.AutoThemeSwitchingEnabled = true;
            builder.Config.Location.Enabled = false;
            NumberboxOffsetDark.Minimum = 0;
            NumberBoxOffsetLight.Minimum = 0;
            try
            {
                if (!init) builder.Save();
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessage(ex, Window.GetWindow(this), "PageTime RadioButtonWindowsNightLight_Click");
            }
        }

        private void PostponeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cb = (ComboBoxItem)PostponeComboBox.SelectedItem;
            string minutesString = cb.Name.Replace("Postpone", "");
            if (int.TryParse(minutesString, out int minutes))
            {
                selectedPostponeMinutes = minutes;
            }
            else
            {
                selectedPostponeMinutes = 0;
            }
        }

        private async void GridNightLight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:nightlight"));
        }
    }
}