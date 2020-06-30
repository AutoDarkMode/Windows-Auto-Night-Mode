using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.System.Power;
using AutoThemeChanger.Properties;
using System.Diagnostics;

namespace AutoThemeChanger.Pages
{
    /// <summary>
    /// Interaction logic for PageTime.xaml
    /// </summary>
    public partial class PageTime : Page
    {
        readonly TaskSchHandler taskSchHandler = new TaskSchHandler();
        readonly RegeditHandler regEditHandler = new RegeditHandler();

        public PageTime()
        {
            InitializeComponent();
            DoesTaskExists();
            if (Settings.Default.AlterTime) AlterTime(true);
        }

        /// <summary>
        /// check if the tasks already exists in task scheduler and get the data from them
        /// </summary>
        private void DoesTaskExists()
        {
            //user has custom hours enabled
            if (taskSchHandler.CheckExistingClass().Equals(1))
            {
                //ui
                autoCheckBox.IsChecked = true;
                RadioButtonCustomTimes.IsChecked = true;
                //get times
                int[] darkStart = taskSchHandler.GetRunTime("dark");
                int[] lightStart = taskSchHandler.GetRunTime("light");
                darkStartBox.Text = Convert.ToString(darkStart[0]);
                if(darkStart[1] < 10)
                {
                    DarkStartMinutesBox.Text = "0" + Convert.ToString(darkStart[1]);
                }
                else
                {
                    DarkStartMinutesBox.Text = Convert.ToString(darkStart[1]);
                }
                lightStartBox.Text = Convert.ToString(lightStart[0]);
                if(lightStart[1] < 10)
                {
                    LightStartMinutesBox.Text = "0" + Convert.ToString(lightStart[1]);
                }
                else
                {
                    LightStartMinutesBox.Text = Convert.ToString(lightStart[1]);
                }
            }
            //user has location sunset and sunrise enabled
            else if (taskSchHandler.CheckExistingClass().Equals(2))
            {
                autoCheckBox.IsChecked = true;
                RadioButtonLocationTimes.IsChecked = true;
                ActivateLocationMode();
                InitOffset();
            }
            //user didn't enabled anything or tasks in scheduler are missing
            else
            {
                AutoCheckBox_Unchecked(this, null);
            }
        }

        //offset for sunrise and sunset hours
        private void PopulateOffsetFields(int offsetDark, int offsetLight)
        {
            if (offsetLight < 0)
            {
                OffsetLightModeButton.Content = "-";
                OffsetLightBox.Text = Convert.ToString(-offsetLight);
            }
            else
            {
                OffsetLightBox.Text = Convert.ToString(offsetLight);
            }
            if (offsetDark < 0)
            {
                OffsetDarkModeButton.Content = "-";
                OffsetDarkBox.Text = Convert.ToString(-offsetDark);
            }
            else
            {
                OffsetDarkBox.Text = Convert.ToString(offsetDark);
            }
        }
        private void InitOffset()
        {
            PopulateOffsetFields(Properties.Settings.Default.DarkOffset, Properties.Settings.Default.LightOffset);
        }
        //+ and - button
        private void OffsetModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content.ToString() == "+")
                {
                    button.Content = "-";
                }
                else
                {
                    button.Content = "+";
                }
                OffsetButton.IsEnabled = true;
            }
        }
        //apply offset
        private void OffsetButton_Click(object sender, RoutedEventArgs e)
        {
            int offsetDark;
            int offsetLight;

            //get values from TextBox
            try
            {
                offsetDark = int.Parse(OffsetDarkBox.Text);
                offsetLight = int.Parse(OffsetLightBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            PopulateOffsetFields(offsetDark, offsetLight);

            if (OffsetLightModeButton.Content.ToString() == "+")
            {
                Properties.Settings.Default.LightOffset = offsetLight;
            }
            else
            {
                Properties.Settings.Default.LightOffset = -offsetLight;
            }

            if (OffsetDarkModeButton.Content.ToString() == "+")
            {
                Properties.Settings.Default.DarkOffset = offsetDark;
            }
            else
            {
                Properties.Settings.Default.DarkOffset = -offsetDark;
            }

            OffsetButton.IsEnabled = false;
            ActivateLocationMode();
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
                darkStart = int.Parse(darkStartBox.Text);
                darkStartMinutes = int.Parse(DarkStartMinutesBox.Text);
                lightStart = int.Parse(lightStartBox.Text);
                lightStartMinutes = int.Parse(LightStartMinutesBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            //check values from TextBox
            //hours with 24 hour time
            if (!Properties.Settings.Default.AlterTime)
            {
                if (darkStart >= 24)
                {
                    darkStart = 23;
                    darkStartMinutes = 59;
                }
                if (lightStart >= darkStart)
                {
                    lightStart = darkStart - 3;
                }
                if (lightStart < 0)
                {
                    lightStart = 6;
                    darkStart = 17;
                }
            }
            //hours with 12 hour time
            else
            {
                if (darkStart >= 12)
                {
                    darkStart = 11;
                    darkStartMinutes = 59;
                }
                if(darkStart == 0)
                {
                    darkStart = 1;
                }
                if (lightStart >= 13)
                {
                    lightStart = 12;
                }
            }

            //minutes
            if (lightStartMinutes > 59)
            {
                lightStartMinutes = 59;
            }
            if (darkStartMinutes > 59)
            {
                darkStartMinutes = 59;
            }

            //display edited hour values for the user
            darkStartBox.Text = Convert.ToString(darkStart);
            lightStartBox.Text = Convert.ToString(lightStart);

            //display minute values + more beautiful display of minutes, if they are under 10
            if (lightStartMinutes < 10)
            {
                LightStartMinutesBox.Text = "0" + Convert.ToString(lightStartMinutes);
            }
            else
            {
                LightStartMinutesBox.Text = Convert.ToString(lightStartMinutes);
            }
            if (darkStartMinutes < 10)
            {
                DarkStartMinutesBox.Text = "0" + Convert.ToString(darkStartMinutes);
            }
            else
            {
                DarkStartMinutesBox.Text = Convert.ToString(darkStartMinutes);
            }

            //Apply Theme
            if (Properties.Settings.Default.AlterTime)
            {
                darkStart += 12;
            }
            ApplyTheme(darkStart, darkStartMinutes, lightStart, lightStartMinutes);

            //ui
            applyButton.IsEnabled = false;
        }

        private void ApplyTheme(int DarkHour, int DarkMinute, int LightHour, int LightMinute)
        {
            //create task scheduler theme switching tasks 
            try
            {
                taskSchHandler.CreateTask(DarkHour, DarkMinute, LightHour, LightMinute);
            }
            catch (UnauthorizedAccessException ex)
            {
                MsgBox msg = new MsgBox(string.Format(Properties.Resources.ErrorApplyRestart, ex), Properties.Resources.errorOcurredTitle, "error", "close");
                msg.Owner = Window.GetWindow(this);
                msg.ShowDialog();

                //run AutoDarkMode.exe /removeTask as admin
                Process proc = new Process();
                proc.StartInfo.FileName = Application.ResourceAssembly.Location;
                proc.StartInfo.Arguments = "/removeTask";
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                proc.Start();

                //restart app
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
                return;
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme("Error ocurred in: taskShedHandler.CreateTask()", ex.Message);
                return;
            }
            //switch the theme now
            try
            {
                regEditHandler.SwitchThemeBasedOnTime();
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme("Error ocurred in: regEditHandler.SwitchThemeBasedOnTime()", ex.Message);
                return;
            }
            //create windows autostart entry
            try
            {
                if (Settings.Default.LogonTaskInsteadOfAutostart)
                {
                    taskSchHandler.CreateLogonTask();
                }
                else
                {
                    regEditHandler.AddAutoStart();
                }
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme("Error ocurred in: taskShedHandler.AddAutoStart()", ex.Message);
                return;
            }
            //add background updater task
            try
            {
                if (Settings.Default.BackgroundUpdate)
                {
                    taskSchHandler.CreateAppUpdaterTask();
                }
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme("Error ocurred in: taskShedHandler.CreateAppUpdaterTask()", ex.Message);
                return;
            }
            //add connected standby task
            try
            {
                if (Properties.Settings.Default.connectedStandby)
                {
                    taskSchHandler.CreateConnectedStandbyTask();
                }
            }
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme("Error ocurred in: taskShedHandler.CreateConnectedStandbyTask()", ex.Message);
            }

            //this setting enables all the configuration possibilities of auto dark mode
            Settings.Default.Enabled = true;

            //show warning for notebook on battery with enabled battery saver
            if (PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved + "\n\n" + Properties.Resources.msgBatterySaver;
                applyButton.IsEnabled = true;
            }
            else
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved;//changes were saved!
            }
        }
        //if something went wrong while applying the settings :(
        private void ErrorWhileApplyingTheme(string erroDescription, string exception)
        {
            userFeedback.Text = Properties.Resources.msgErrorOcc;
            string error = Properties.Resources.errorThemeApply + "\n\n" + erroDescription + "\n\n" + exception;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
            }
        }

        // set starttime based on user location
        public async void ActivateLocationMode()
        {
            //ui
            StackPanelTimePicker.Visibility = Visibility.Collapsed;
            TextBlockDark.Visibility = Visibility.Collapsed;
            TextBlockLight.Visibility = Visibility.Collapsed;
            StackPanelLocationTime.Visibility = Visibility.Visible;
            SetOffsetVisibility(Visibility.Visible);
            locationBlock.Visibility = Visibility.Visible;
            locationBlock.Text = Properties.Resources.msgSearchLoc;//Searching your location...
            userFeedback.Text = Properties.Resources.msgSearchLoc;

            LocationHandler locationHandler = new LocationHandler();
            var accesStatus = await Geolocator.RequestAccessAsync();
            switch (accesStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    //locate user + get sunrise & sunset times
                    locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                    int[] sundate = await locationHandler.CalculateSunTime(false);

                    //apply settings
                    ApplyTheme(sundate[2], sundate[3], sundate[0], sundate[1]);

                    //show time in UI
                    if (Properties.Settings.Default.AlterTime)
                    {
                        sundate[2] -= 12;
                    }

                    TimeSpan TimeForUiLight = new TimeSpan(sundate[0], sundate[1], 0);
                    TimeSpan TimeForUiDark = new TimeSpan(sundate[2], sundate[3], 0);
                    TextBlockLightTime.Text = Properties.Resources.lblLight + ": " + string.Format("{0:00}:{1:00}", TimeForUiLight.Hours, TimeForUiLight.Minutes); //textblock1
                    TextBlockDarkTime.Text = Properties.Resources.lblDark + ": " + string.Format("{0:00}:{1:00}", TimeForUiDark.Hours, TimeForUiDark.Minutes); //textblock2

                    // ui controls
                    lightStartBox.IsEnabled = false;
                    LightStartMinutesBox.IsEnabled = false;
                    darkStartBox.IsEnabled = false;
                    DarkStartMinutesBox.IsEnabled = false;

                    applyButton.Visibility = Visibility.Hidden;
                    taskSchHandler.CreateLocationTask();
                    break;

                case GeolocationAccessStatus.Denied:
                    NoLocationAccess();
                    break;

                case GeolocationAccessStatus.Unspecified:
                    NoLocationAccess();
                    break;
            }
            return;
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
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.Visibility = Visibility.Visible;
            applyButton.IsEnabled = true;
            locationBlock.Visibility = Visibility.Collapsed;
            StackPanelLocationTime.Visibility = Visibility.Collapsed;
            StackPanelTimePicker.Visibility = Visibility.Visible;
            TextBlockDark.Visibility = Visibility.Visible;
            TextBlockLight.Visibility = Visibility.Visible;
            SetOffsetVisibility(Visibility.Collapsed);

            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            taskSchHandler.RemoveLocationTask();
        }

        //automatic theme switch checkbox
        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StackPanelRadioHolder.IsEnabled = true;
            RadioButtonCustomTimes.IsChecked = true;
            applyButton.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                taskSchHandler.RemoveAllTasks();
                if (Settings.Default.LogonTaskInsteadOfAutostart)
                {
                    taskSchHandler.RemoveLogonTask();
                } 
                else
                {
                    regEditHandler.RemoveAutoStart();
                }
            }

            StackPanelRadioHolder.IsEnabled = false;
            RadioButtonCustomTimes.IsChecked = true;
            DisableLocationMode();
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
            Settings.Default.Enabled = false;
        }

        //12 hour times
        private void AlterTime(bool enable)
        {
            if (enable)
            {
                Properties.Settings.Default.AlterTime = true;
                amTextBlock.Text = "am";
                pmTextBlock.Text = "pm";
                TextBlockDark.Margin = new Thickness(113,0,0,0);
                int darkTime = Convert.ToInt32(darkStartBox.Text) - 12;
                if (darkTime < 1)
                {
                    darkTime = 7;
                }
                darkStartBox.Text = Convert.ToString(darkTime);

                int lightTime = Convert.ToInt32(lightStartBox.Text);
                if (lightTime > 12)
                {
                    lightTime = 7;
                }
                lightStartBox.Text = Convert.ToString(lightTime);
            }
            else
            {
                Properties.Settings.Default.AlterTime = false;
                amTextBlock.Text = "";
                pmTextBlock.Text = "";
                applyButton.Margin = new Thickness(184, 25, 0, 0);
                int darkTime = Convert.ToInt32(darkStartBox.Text) + 12;
                if (darkTime > 24)
                {
                    darkTime = 19;
                }
                if (darkTime == 24)
                {
                    darkTime = 23;
                }
                darkStartBox.Text = Convert.ToString(darkTime);
            }

        }
        private void SetOffsetVisibility(Visibility value)
        {
            OffsetLbl.Visibility = value;
            OffsetDarkLbl.Visibility = value;
            OffsetDarkModeButton.Visibility = value;
            OffsetLightLbl.Visibility = value;
            OffsetLightModeButton.Visibility = value;
            OffsetLightBox.Visibility = value;
            OffsetDarkBox.Visibility = value;
            OffsetDarkDot.Visibility = value;
            OffsetLightDot.Visibility = value;
            OffsetButton.Visibility = value;
        }

        private void RadioButtonCustomTimes_Click(object sender, RoutedEventArgs e)
        {
            DisableLocationMode();
        }

        private void RadioButtonLocationTimes_Click(object sender, RoutedEventArgs e)
        {
            ActivateLocationMode();
        }

        //textbox event handlers
        private void TextBox_BlockChars_TextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            OffsetButton.IsEnabled = true;
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
        private void TextBox_TabNext_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (((TextBox)sender).MaxLength == ((TextBox)sender).Text.Length)
            {
                var ue = e.OriginalSource as FrameworkElement;
                e.Handled = true;
                ue.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void TextBlockHelpWiki_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Troubleshooting");
        }
    }
}
