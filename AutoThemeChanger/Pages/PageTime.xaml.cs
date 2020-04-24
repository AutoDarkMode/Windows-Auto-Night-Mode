using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.System.Power;

namespace AutoThemeChanger.Pages
{
    /// <summary>
    /// Interaction logic for PageTime.xaml
    /// </summary>
    public partial class PageTime : Page
    {
        TaskSchHandler taskShedHandler = new TaskSchHandler();
        RegeditHandler regEditHandler = new RegeditHandler();

        public PageTime()
        {
            InitializeComponent();
            DoesTaskExists();
            if (Properties.Settings.Default.AlterTime) AlterTime(true);
        }
        private void DoesTaskExists()
        {
            if (taskShedHandler.CheckExistingClass().Equals(1))
            {
                autoCheckBox.IsChecked = true;
                int[] darkStart = taskShedHandler.GetRunTime("dark");
                int[] lightStart = taskShedHandler.GetRunTime("light");
                darkStartBox.Text = Convert.ToString(darkStart[0]);
                DarkStartMinutesBox.Text = Convert.ToString(darkStart[1]);
                lightStartBox.Text = Convert.ToString(lightStart[0]);
                LightStartMinutesBox.Text = Convert.ToString(lightStart[1]);
            }
            else if (taskShedHandler.CheckExistingClass().Equals(2))
            {
                autoCheckBox.IsChecked = true;
                locationCheckBox.IsChecked = true;
                InitOffset();
            }
            else
            {
                AutoCheckBox_Unchecked(this, null);
            }
        }

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
            GetLocation();
        }


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
            else
            {
                if (darkStart >= 12)
                {
                    darkStart = 11;
                    darkStartMinutes = 59;
                }
                if (lightStart >= 13)
                {
                    lightStart = 12;
                }
            }

            if (lightStartMinutes > 59)
            {
                lightStartMinutes = 59;
            }
            if (darkStartMinutes > 59)
            {
                darkStartMinutes = 59;
            }
            darkStartBox.Text = Convert.ToString(darkStart);
            lightStartBox.Text = Convert.ToString(lightStart);
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

            try
            {
                if (Properties.Settings.Default.AlterTime)
                {
                    darkStart += 12;
                }
                taskShedHandler.CreateTask(darkStart, darkStartMinutes, lightStart, lightStartMinutes);
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
                }
                return;
            }
            try
            {
                regEditHandler.SwitchThemeBasedOnTime();
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: regEditHandler.SwitchThemeBasedOnTime()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
                }
                return;
            }
            try
            {
                regEditHandler.AddAutoStart();
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: regEditHandler.AddAutoStart()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
                }
                return;
            }
            try
            {
                if (Properties.Settings.Default.BackgroundUpdate)
                {
                    taskShedHandler.CreateAppUpdaterTask();
                }
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateAppUpdaterTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
                }
                return;
            }
            try
            {
                if (Properties.Settings.Default.connectedStandby)
                {
                    taskShedHandler.CreateConnectedStandbyTask();
                }
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateConnectedStandbyTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
                }
            }

            applyButton.IsEnabled = false;
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

        //textbox event handler
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
            textBox.Dispatcher.BeginInvoke(new Action(() =>
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

        // set starttime based on user location
        private void LocationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GetLocation();
        }
        public async void GetLocation()
        {
            SetOffsetVisibility(Visibility.Visible);
            locationBlock.Visibility = Visibility.Visible;
            locationBlock.Text = Properties.Resources.msgSearchLoc;//Searching your location...
            LocationHandler locationHandler = new LocationHandler();

            var accesStatus = await Geolocator.RequestAccessAsync();
            switch (accesStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    //locate user + get sunrise & sunset times
                    locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                    int[] sundate = await locationHandler.CalculateSunTime(false);

                    //apply settings & change UI
                    lightStartBox.Text = sundate[0].ToString();
                    LightStartMinutesBox.Text = sundate[1].ToString();
                    if (Properties.Settings.Default.AlterTime)
                    {
                        sundate[2] -= 12;
                        darkStartBox.Text = sundate[2].ToString();
                    }
                    else
                    {
                        darkStartBox.Text = sundate[2].ToString();
                    }
                    DarkStartMinutesBox.Text = sundate[3].ToString();
                    lightStartBox.IsEnabled = false;
                    LightStartMinutesBox.IsEnabled = false;
                    darkStartBox.IsEnabled = false;
                    DarkStartMinutesBox.IsEnabled = false;
                    applyButton.IsEnabled = false;
                    ApplyButton_Click(this, null);
                    taskShedHandler.CreateLocationTask();
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
            locationCheckBox.IsChecked = false;
            locationBlock.Text = Properties.Resources.msgLocPerm;//The App needs permission to location
            locationBlock.Visibility = Visibility.Visible;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        private void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            locationBlock.Visibility = Visibility.Collapsed;
            SetOffsetVisibility(Visibility.Collapsed);

            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            taskShedHandler.RemoveLocationTask();
        }

        //automatic theme switch checkbox
        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            locationCheckBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            Properties.Settings.Default.Enabled = true;
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                taskShedHandler.RemoveTask();
                regEditHandler.RemoveAutoStart();
            }

            Properties.Settings.Default.WallpaperSwitch = false;
            Properties.Settings.Default.WallpaperLight = null;
            Properties.Settings.Default.WallpaperDark = null;

            Properties.Settings.Default.ThemeSwitch = false;
            Properties.Settings.Default.ThemeLight = null;
            Properties.Settings.Default.ThemeDark = null;

            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
            Properties.Settings.Default.Enabled = false;
        }
        private void AlterTime(bool enable)
        {
            if (enable)
            {
                Properties.Settings.Default.AlterTime = true;
                amTextBlock.Text = "am";
                pmTextBlock.Text = "pm";
                applyButton.Margin = new Thickness(205, 25, 0, 0);
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
    }
}
