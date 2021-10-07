using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.System.Power;
using AutoDarkModeApp.Properties;
using System.Diagnostics;
using AutoDarkModeConfig;
using System.Globalization;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;
using AutoDarkModeComms;
using AutoDarkModeApp.Handlers;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageTime.xaml
    /// </summary>
    public partial class PageTime : Page
    {
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        readonly ICommandClient messagingClient = new ZeroMQClient(Address.DefaultPort);
        private readonly bool init = true;
        private delegate void DispatcherDelegate();

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
                ShowErrorMessage(ex);
            }

            //initialize ui components
            InitializeComponent();

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

            //read datetime from config file
            TimePickerDark.SelectedDateTime = builder.Config.Sunset;
            TimePickerLight.SelectedDateTime = builder.Config.Sunrise;

            //read offset from config file
            NumberBoxOffsetLight.Value = Convert.ToDouble(builder.Config.Location.SunriseOffsetMin);
            NumberboxOffsetDark.Value = Convert.ToDouble(builder.Config.Location.SunsetOffsetMin);

            //read coordinates from config file
            NumberBoxLat.Value = builder.Config.Location.CustomLat;
            NumberBoxLon.Value = builder.Config.Location.CustomLon;

            //tick correct radio button and prepare UI
            //is auto theme switch enabled?
            //disabled
            if (!builder.Config.AutoThemeSwitchingEnabled)
            {
                DisableTimeBasedSwitch();
                TogglePanelVisibility(true, false, false, false);
                RadioButtonDisabled.IsChecked = true;
            }
            //enabled
            else
            {
                //is custom timepicker input enabled?
                if (!builder.Config.Location.Enabled)
                {
                    RadioButtonCustomTimes.IsChecked = true;
                    TogglePanelVisibility(true, false, false, false);
                    applyButton.IsEnabled = false;
                }

                //is location mode enabled?
                if (builder.Config.Location.Enabled)
                {
                    //windows location service
                    if (builder.Config.Location.UseGeolocatorService)
                    {
                        TogglePanelVisibility(false, true, true, false);
                        ActivateLocationMode();
                        RadioButtonLocationTimes.IsChecked = true;
                    }
                    //custom geographic coordinates
                    else
                    {
                        RadioButtonCoordinateTimes.IsChecked = true;
                        TogglePanelVisibility(false, true, true, true);
                        ActivateLocationMode();
                    }
                }
            }
            init = false;
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
                userFeedback.Text = Properties.Resources.errorNumberInput;
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
                builder.Save();
            } 
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "OffsetButton_Click");
            }
        }


        private void TogglePanelVisibility(bool timepicker, bool location, bool offset, bool coordinates)
        {
            if (timepicker)
            {
                GridTimePicker.Visibility = Visibility.Visible;
            }
            else
            {
                GridTimePicker.Visibility = Visibility.Collapsed;
            }

            if (location)
            {
                GridLocationTimeInfo.Visibility = Visibility.Visible;
            }
            else
            {
                GridLocationTimeInfo.Visibility = Visibility.Collapsed;
            }

            if (offset)
            {
                GridOffset.Visibility = Visibility.Visible;
            }
            else
            {
                GridOffset.Visibility = Visibility.Collapsed;
            }
            if (coordinates)
            {
                GridCoordinates.Visibility = Visibility.Visible;
            }
            else
            {
                GridCoordinates.Visibility = Visibility.Collapsed;
            }
        }

        //apply theme
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int darkStart;
            int darkStartMinutes;
            int lightStart;
            int lightStartMinutes;

            //get values from TextBox
            try
            {
                darkStart = TimePickerDark.SelectedDateTime.Value.Hour;
                darkStartMinutes = TimePickerDark.SelectedDateTime.Value.Minute;
                lightStart = TimePickerLight.SelectedDateTime.Value.Hour;
                lightStartMinutes = TimePickerLight.SelectedDateTime.Value.Minute;

            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            //check values from timepicker

            //currently nothing to check

            //display edited hour values for the user
            //disabled because we don't check and edit anything
            //TimePickerDark.SelectedDateTime = new DateTime(2000, 08, 22, darkStart, darkStartMinutes, 0);
            //TimePickerLight.SelectedDateTime = new DateTime(2000, 08, 22, lightStart, lightStartMinutes, 0);

            //Apply Theme
            builder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStart, darkStartMinutes, 0);
            builder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStart, lightStartMinutes, 0);

            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));

            //ui
            //applyButton.IsEnabled = false;
        }

        private async void ApplyTheme()
        {
            //show warning for notebook on battery with enabled battery saver
            if (!builder.Config.Tunable.DisableEnergySaverOnThemeSwitch && PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved + "\n\n" + Properties.Resources.msgBatterySaver;
                applyButton.IsEnabled = true;
            }
            else
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved;//changes were saved!
            }

            try
            {
                string result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch, 15);
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
            userFeedback.Text = Properties.Resources.msgErrorOcc;
            string error = string.Format(Properties.Resources.errorThemeApply, Properties.Resources.cbSettingsMultiUserImprovements) + "\n\n" + erroDescription + "\n\n" + exception;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            _ = msg.ShowDialog();
            bool? result = msg.DialogResult;
            if (result == true)
            {
                StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
            }
        }

        /// <summary>
        /// Location based times & Windows Position Service
        /// </summary>
        // set starttime based on user location
        public async void ActivateLocationMode()
        {
            //ui
            locationBlock.Text = Properties.Resources.msgSearchLoc;//Searching your location...
            userFeedback.Text = Properties.Resources.msgSearchLoc;
            
            await LoadGeolocationData();
            UpdateSuntimes();

            // ui controls
            userFeedback.Text = Properties.Resources.msgChangesSaved;

            return;
        }

        private async Task LoadGeolocationData()
        {
            int maxTries = 5;
            for (int i = 0; i < maxTries; i++)
            {
                ApiResponse result = ApiResponse.FromString(await messagingClient.SendMessageAndGetReplyAsync(Command.GeolocatorIsUpdating));
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
                ShowErrorMessage(ex);
            }

            try
            {
                ApiResponse result = ApiResponse.FromString(await messagingClient.SendMessageAndGetReplyAsync(Command.LocationAccess));
                LocationHandler handler = new();
                if (builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.NoLocAccess)
                {
                    NoLocationAccess();
                    return;
                }
                else if (builder.Config.Location.UseGeolocatorService && result.StatusCode == StatusCode.Ok)
                {
                    locationBlock.Text = Properties.Resources.lblCity + ": " + await handler.GetCityName();
                }
                else if (!builder.Config.Location.UseGeolocatorService)
                {
                    locationBlock.Text = $"{Properties.Resources.lblPosition}: Lat {Math.Round(builder.LocationData.Lat, 3)} / Lon {Math.Round(builder.LocationData.Lon, 3)}";
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
                return;
            }
        }

        private async void NoLocationAccess()
        {
            locationBlock.Text = Properties.Resources.msgLocPerm;//The App needs permission to location
            userFeedback.Text = Properties.Resources.msgLocPerm;
            locationBlock.Visibility = Visibility.Visible;
            TextBlockDarkTime.Text = null;
            TextBlockLightTime.Text = null;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void DisableLocationMode()
        {
            TogglePanelVisibility(true, false, false, false);

            builder.Config.Location.Enabled = false;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
        }


        private void EnableTimeBasedSwitch()
        {
            // check the right radio button
            //RadioButtonCustomTimes.IsChecked = !builder.Config.Location.Enabled;
            //RadioButtonLocationTimes.IsChecked = builder.Config.Location.Enabled && builder.Config.Location.UseGeolocatorService;
            //RadioButtonCustomTimes.IsChecked = builder.Config.Location.Enabled && !builder.Config.Location.UseGeolocatorService;

            StackPanelTimePicker.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes

            //this setting enables all the configuration possibilities of auto dark mode
            if (!builder.Config.AutoThemeSwitchingEnabled)
            {
                builder.Config.AutoThemeSwitchingEnabled = true;
                EnableAutoStart();
            }
        }
        private void DisableTimeBasedSwitch()
        {
            if (!init)
            {
                //disable auto theme switching in svc
                builder.Config.AutoThemeSwitchingEnabled = false;
                builder.Config.Location.Enabled = false;
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex, "DisableTimeBasedSwitch");
                }
            }

            StackPanelTimePicker.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
        }

        /// <summary>
        /// Autostart
        /// </summary>
        private async void EnableAutoStart()
        {
            string result = "";
            try
            {
                builder.Save();
                result = await messagingClient.SendMessageAndGetReplyAsync(Command.AddAutostart);
                if (result != StatusCode.Ok)
                {
                    throw new AddAutoStartException($"Could not add Auto Dark Mode to autostart", "AutoCheckBox_Checked");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBoxes.ShowErrorMessageFromApi(ApiResponse.FromString(result), ex, Window.GetWindow(this));
            }
        }
        private async void DisableAutoStart()
        {
            //remove autostart
            try
            {
                var result = await messagingClient.SendMessageAndGetReplyAsync(Command.RemoveAutostart);
                if (result != StatusCode.Ok)
                {
                    throw new AddAutoStartException($"ZMQ command {result}", "AutoCheckBox_Checked");
                }
            }
            catch (AddAutoStartException aex)
            {
                ShowErrorMessage(aex);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }


        private void UpdateSuntimes()
        {
            LocationHandler.GetSunTimesWithOffset(builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
            if (Settings.Default.AlterTime)
            {
                TextBlockLightTime.Text = Properties.Resources.lblLight + ": " + SunriseWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = Properties.Resources.lblDark + ": " + SunsetWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture); //textblock2
            }
            else
            {
                TextBlockLightTime.Text = Properties.Resources.lblLight + ": " + SunriseWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = Properties.Resources.lblDark + ": " + SunsetWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture); //textblock2
            }
        }

        /// <summary>
        /// Geographic Coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonApplyCoordinates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                builder.Config.Location.CustomLat = NumberBoxLat.Value;
                builder.Config.Location.CustomLon = NumberBoxLon.Value;
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }
            builder.Config.Location.Enabled = true;
            builder.Config.Location.UseGeolocatorService = false;
            EnableTimeBasedSwitch();
            try
            {
                builder.Save();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            ButtonApplyCoordinates.IsEnabled = false;
            ActivateLocationMode();
            Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            TogglePanelVisibility(false, true, true, true);
        }

        private void ShowErrorMessage(Exception ex, string location = "PageTime")
        {
            string error = Properties.Resources.errorThemeApply + $"\n\nError ocurred in: {location}" + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
            bool result = msg.DialogResult ?? false;
            if (result)
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


        /// <summary>
        /// radio buttons
        /// </summary>

        private void RadioButtonDisabled_Click(object sender, RoutedEventArgs e)
        {
            DisableTimeBasedSwitch();
            TogglePanelVisibility(true, false, false, false);
        }

        private void RadioButtonCustomTimes_Click(object sender, RoutedEventArgs e)
        {
            EnableTimeBasedSwitch();
            DisableLocationMode();
            //applyButton.IsEnabled = true;
            try
            {
                builder.Save();
                Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RadioButtonCustomTimes_Click");
            }
        }

        private void RadioButtonLocationTimes_Click(object sender, RoutedEventArgs e)
        {
            EnableTimeBasedSwitch();
            builder.Config.Location.Enabled = true;
            builder.Config.Location.UseGeolocatorService = true;
            try
            {
                builder.Save();
                TogglePanelVisibility(false, true, true, false);
                ActivateLocationMode();
                Dispatcher.BeginInvoke(new DispatcherDelegate(ApplyTheme));
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex, "RadioButtonCustomTimes_Click");
            }

        }

        private void RadioButtonCoordinateTimes_Click(object sender, RoutedEventArgs e)
        {
            if (builder.Config.Location.CustomLat != 0 & builder.Config.Location.CustomLon != 0)
            {
                TogglePanelVisibility(false, false, true, true);
                ButtonApplyCoordinates_Click(this, null);
            }
            else
            {
                TogglePanelVisibility(false, false, false, true);
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
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Troubleshooting");
        }

        private void TextBlockHelpWiki_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockHelpWiki_MouseDown(this, null);
            }
        }

        //numbox event handler
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockCopyPaste_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TexttBox_SelectAll_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                textBox.SelectAll();
            }));
        }

        private void NumberBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void NumberBox_ValueChanged(ModernWpf.Controls.NumberBox sender, ModernWpf.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!init)
            {
                if(sender.Tag.Equals("offset"))
                {
                    if (OffsetButton != null)
                    {
                        OffsetButton.IsEnabled = true;
                    }
                }
                else if (sender.Tag.Equals("coordinates"))
                {
                    if(ButtonApplyCoordinates != null)
                    {
                        ButtonApplyCoordinates.IsEnabled = true;
                    }
                }
                userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            }
        }

        private void NumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!init)
            {
                if((sender as ModernWpf.Controls.NumberBox).Tag.Equals("offset"))
                {
                    if (OffsetButton != null)
                    {
                        OffsetButton.IsEnabled = true;
                    }
                }
                else if ((sender as ModernWpf.Controls.NumberBox).Tag.Equals("coordinates"))
                {
                    if (ButtonApplyCoordinates != null)
                    {
                        ButtonApplyCoordinates.IsEnabled = true;
                    }
                }
                userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            }
        }

        private void TextBlockOpenLatLongWebsite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://www.latlong.net/");
        }

        private void TextBlockOpenLatLongWebsite_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter | e.Key == Key.Space)
            {
                TextBlockOpenLatLongWebsite_MouseDown(this, null);
            }
        }
    }
}