using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using Windows.Devices.Geolocation;
using Windows.System.Power;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using AutoDarkModeApp.Communication;
using NetMQ;

namespace AutoDarkModeApp
{
    public partial class MainWindow
    {
        private readonly RegeditHandler regEditHandler = new RegeditHandler();
        private readonly AutoDarkModeConfigBuilder autoDarkModeConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        private ICommandClient CommandClient { get; }



        private readonly bool is1903 = false;

        public MainWindow()
        {
            Console.WriteLine("--------- AppStart");

            // Read json config file
            autoDarkModeConfigBuilder.Read();
            CommandClient = new ZeroMQClient(Tools.DefaultPort);

            LanguageHelper();
            InitializeComponent();
            if (int.Parse(regEditHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            DoesTaskExists();
            UiHandler();
            ThemeChange(this, null);
            SourceChord.FluentWPF.SystemTheme.ThemeChanged += ThemeChange;
            if (Properties.Settings.Default.FirstRun)
            {
                SystemTimeFormat();
                AddJumpList();
                Properties.Settings.Default.FirstRun = false;
            }
            if (Properties.Settings.Default.AlterTime) AlterTime(true);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Updater updater = new Updater();
            updater.CheckNewVersion();
            LanguageHelper();
            DonationScreen();
        }

        private void DonationScreen()
        {
            Random rdmnumber = new Random();
            int generatedNumber = rdmnumber.Next(1, 100);
            if (generatedNumber == 50)
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.donationDescription, Properties.Resources.donationTitle, "smiley", "yesno");
                msgBox.Owner = GetWindow(this);
                msgBox.ShowDialog();
                var result = msgBox.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://www.paypal.me/arminosaj");
                }
            }
        }

        private void LanguageHelper()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.Language.ToString()))
            {
                Properties.Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language, true);
        }

        private void SystemTimeFormat()
        {
            try
            {
                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                sysFormat = sysFormat.Substring(0, sysFormat.IndexOf(":"));
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    Properties.Settings.Default.AlterTime = true;
                }
            }
            catch
            {

            }
        }

        private void ThemeChange(object sender, EventArgs e)
        {
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark))
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"Resources\Microsoft_Edge_Logo_White.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"Resources\Microsoft_Edge_Logo.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void DoesTaskExists()
        {
            if (TaskSchdHandler.CheckExistingClass().Equals(1))
            {
                autoCheckBox.IsChecked = true;
                int[] darkStart = TaskSchdHandler.GetRunTime("dark");
                int[] lightStart = TaskSchdHandler.GetRunTime("light");
                darkStartBox.Text = Convert.ToString(darkStart[0]);
                DarkStartMinutesBox.Text = Convert.ToString(darkStart[1]);
                lightStartBox.Text = Convert.ToString(lightStart[0]);
                LightStartMinutesBox.Text = Convert.ToString(lightStart[1]);
            }
            else if (TaskSchdHandler.CheckExistingClass().Equals(2))
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

        private void UiHandler()
        {
            int appTheme = autoDarkModeConfigBuilder.Config.AppsTheme;
            Console.WriteLine("appTheme Value: " + appTheme);
            if (appTheme == 0) AppComboBox.SelectedIndex = 0;
            if (appTheme == 1) AppComboBox.SelectedIndex = 1;
            if (appTheme == 2) AppComboBox.SelectedIndex = 2;

            int systemTheme = autoDarkModeConfigBuilder.Config.SystemTheme;
            Console.WriteLine("SystemTheme Value: " + systemTheme);
            if (systemTheme == 0) SystemComboBox.SelectedIndex = 0;
            if (systemTheme == 1) SystemComboBox.SelectedIndex = 1;
            if (systemTheme == 2) SystemComboBox.SelectedIndex = 2;

            int edgeTheme = autoDarkModeConfigBuilder.Config.EdgeTheme;
            Console.WriteLine("EdgeTheme Value: " + edgeTheme);
            if (edgeTheme == 0) EdgeComboBox.SelectedIndex = 0;
            if (edgeTheme == 1) EdgeComboBox.SelectedIndex = 1;
            if (edgeTheme == 2) EdgeComboBox.SelectedIndex = 2;
            if (edgeTheme == 3) EdgeComboBox.SelectedIndex = 3;

            if (!is1903)
            {
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.cmb1903;
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.cmb1903;
            }
            else
            {
                AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;
            }

            if (autoDarkModeConfigBuilder.Config.AccentColorTaskbar)
            {
                AccentColorCheckBox.IsChecked = true;
            }

            ShowDeskBGStatus();
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
            PopulateOffsetFields(autoDarkModeConfigBuilder.Config.Location.SunsetOffsetMin, autoDarkModeConfigBuilder.Config.Location.SunriseOffsetMin);
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
                autoDarkModeConfigBuilder.Config.Location.SunriseOffsetMin = offsetLight;
                Properties.Settings.Default.LightOffset = offsetLight;
            }
            else
            {
                autoDarkModeConfigBuilder.Config.Location.SunriseOffsetMin = -offsetLight;
                Properties.Settings.Default.LightOffset = -offsetLight;
            }

            if (OffsetDarkModeButton.Content.ToString() == "+")
            {
                autoDarkModeConfigBuilder.Config.Location.SunsetOffsetMin = offsetDark;
                Properties.Settings.Default.DarkOffset = offsetDark;
            }
            else
            {
                autoDarkModeConfigBuilder.Config.Location.SunsetOffsetMin = -offsetDark;
                Properties.Settings.Default.DarkOffset = -offsetDark;
            }

            OffsetButton.IsEnabled = false;
            GetLocation();
            autoDarkModeConfigBuilder.Write();
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

            autoDarkModeConfigBuilder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStart, lightStartMinutes, 0);
            autoDarkModeConfigBuilder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStart, darkStartMinutes, 0);

            try
            {
                if (Properties.Settings.Default.AlterTime)
                {
                    darkStart += 12;
                }
                TaskSchdHandler.CreateTask(darkStart, darkStartMinutes, lightStart, lightStartMinutes);
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
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
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
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
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
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
                if (!autoDarkModeConfigBuilder.Config.Wallpaper.Disabled)
                {
                    TaskSchdHandler.CreateAppUpdaterTask();
                }
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateAppUpdaterTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
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
                    TaskSchdHandler.CreateConnectedStandbyTask();
                }
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: taskShedHandler.CreateConnectedStandbyTask()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
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
            autoDarkModeConfigBuilder.Write();
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

        //open aboutWindow
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = GetWindow(this)
            };
            aboutWindow.ShowDialog();

            if (aboutWindow.AlterTimeCheckBox.IsChecked == true && Properties.Settings.Default.AlterTime == false)
            {
                AlterTime(true);
            }
            else if (aboutWindow.AlterTimeCheckBox.IsChecked == false && Properties.Settings.Default.AlterTime == true)
            {
                AlterTime(false);
            }

            if (aboutWindow.BckgrUpdateCB.IsChecked == true && Properties.Settings.Default.BackgroundUpdate == false)
            {
                TaskSchdHandler.CreateAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = true;
            }
            else if (aboutWindow.BckgrUpdateCB.IsChecked == false && Properties.Settings.Default.BackgroundUpdate == true)
            {
                TaskSchdHandler.RemoveAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = false;
            }

            if (aboutWindow.conStandByCB.IsChecked == true && Properties.Settings.Default.connectedStandby == false)
            {
                TaskSchdHandler.CreateConnectedStandbyTask();
                Properties.Settings.Default.connectedStandby = true;
            }
            else if (aboutWindow.conStandByCB.IsChecked == false && Properties.Settings.Default.connectedStandby == true)
            {
                TaskSchdHandler.RemoveConnectedStandbyTask();
                Properties.Settings.Default.connectedStandby = false;
            }
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            autoDarkModeConfigBuilder.Write();
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
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
                    autoDarkModeConfigBuilder.Config.Location.Disabled = false;

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
                    TaskSchdHandler.CreateLocationTask();
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
            autoDarkModeConfigBuilder.Config.Location.Disabled = true;
            locationCheckBox.IsChecked = false;
            locationBlock.Text = Properties.Resources.msgLocPerm;//The App needs permission to location
            locationBlock.Visibility = Visibility.Visible;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        private void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoDarkModeConfigBuilder.Config.Location.Disabled = true;

            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            locationBlock.Visibility = Visibility.Collapsed;
            SetOffsetVisibility(Visibility.Collapsed);

            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            TaskSchdHandler.RemoveLocationTask();
        }

        //automatic theme switch checkbox
        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            autoDarkModeConfigBuilder.Config.Enabled = true;


            if (is1903) SystemComboBox.IsEnabled = true;
            if (is1903 && !SystemComboBox.SelectedIndex.Equals(1)) AccentColorCheckBox.IsEnabled = true;
            AppComboBox.IsEnabled = true;
            EdgeComboBox.IsEnabled = true;
            locationCheckBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            BGWinButton.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoDarkModeConfigBuilder.Config.Enabled = false;

            if (e != null)
            {
                TaskSchdHandler.RemoveTask();
                regEditHandler.RemoveAutoStart();
            }

            Properties.Settings.Default.WallpaperSwitch = false;
            Properties.Settings.Default.WallpaperLight = "";
            Properties.Settings.Default.WallpaperDark = "";

            autoDarkModeConfigBuilder.Config.Wallpaper.Disabled = true;
            autoDarkModeConfigBuilder.Config.Wallpaper.DarkThemeWallpapers.Clear();
            autoDarkModeConfigBuilder.Config.Wallpaper.LightThemeWallpapers.Clear();


            AccentColorCheckBox.IsEnabled = false;
            SystemComboBox.IsEnabled = false;
            AppComboBox.IsEnabled = false;
            EdgeComboBox.IsEnabled = false;
            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            BGWinButton.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
            ShowDeskBGStatus();
        }

        //ComboBox
        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            autoDarkModeConfigBuilder.Config.AppsTheme = AppComboBox.SelectedIndex;

            if (AppComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.AppThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
            }
            if (AppComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.AppThemeChange = 1;
                regEditHandler.AppTheme(1);
            }
            if (AppComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.AppThemeChange = 2;
                regEditHandler.AppTheme(0);
            }
        }
        private void SystemComboBox_DropDownClosed(object sender, EventArgs e)
        {
            autoDarkModeConfigBuilder.Config.SystemTheme = SystemComboBox.SelectedIndex;


            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.SystemThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
                AccentColorCheckBox.IsEnabled = true;
            }
            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.SystemThemeChange = 1;
                if (autoDarkModeConfigBuilder.Config.AccentColorTaskbar)
                {
                    regEditHandler.ColorPrevalence(0);
                    Thread.Sleep(200);
                }
                regEditHandler.SystemTheme(1);
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.IsChecked = false;
            }
            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                regEditHandler.SystemTheme(0);
                if (autoDarkModeConfigBuilder.Config.AccentColorTaskbar)
                {
                    Thread.Sleep(200);
                    regEditHandler.ColorPrevalence(1);
                }
                AccentColorCheckBox.IsEnabled = true;
            }
        }
        private void EdgeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            autoDarkModeConfigBuilder.Config.EdgeTheme = EdgeComboBox.SelectedIndex;

            if (EdgeComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.EdgeThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
            }
            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.EdgeThemeChange = 1;
                regEditHandler.EdgeTheme(0);
            }
            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.EdgeThemeChange = 2;
                regEditHandler.EdgeTheme(1);
            }
            if (EdgeComboBox.SelectedIndex.Equals(3))
            {
                Properties.Settings.Default.EdgeThemeChange = 3;
            }
        }

        private void AddJumpList()
        {
            JumpTask darkJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblDarkTheme,//Dark theme
                Arguments = "/dark",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };
            JumpTask lightJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblLightTheme,//Light theme
                Arguments = "/light",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };

            JumpList jumpList = new JumpList();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;
            JumpList.SetJumpList(Application.Current, jumpList);
        }

        private void AccentColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            autoDarkModeConfigBuilder.Config.AccentColorTaskbar = true;
            try
            {
                if (SystemComboBox.SelectedIndex.Equals(0)) regEditHandler.SwitchThemeBasedOnTime();
                if (SystemComboBox.SelectedIndex.Equals(2)) regEditHandler.ColorPrevalence(1);
            }
            catch
            {

            }
        }

        private void AccentColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            autoDarkModeConfigBuilder.Config.AccentColorTaskbar = false;
            regEditHandler.ColorPrevalence(0);
        }

        //open desktop background window
        private void BGWinButton_Click(object sender, RoutedEventArgs e)
        {
            DesktopBGui BGui = new DesktopBGui
            {
                Owner = GetWindow(this)
            };
            BGui.ShowDialog();
            if (BGui.saved == true)
            {
                ApplyButton_Click(this, null);
            }
            ShowDeskBGStatus();
        }
        private void ShowDeskBGStatus()
        {
            if(!autoDarkModeConfigBuilder.Config.Wallpaper.Disabled)
            {
                DeskBGStatus.Text = Properties.Resources.enabled;
            }
            else
            {
                DeskBGStatus.Text = Properties.Resources.disabled;
            }
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (autoDarkModeConfigBuilder.Config.ClassicMode) CommandClient.SendMessage(Tools.Shutdown);
            NetMQConfig.Cleanup();
            base.OnClosing(e);
        }
    }
}