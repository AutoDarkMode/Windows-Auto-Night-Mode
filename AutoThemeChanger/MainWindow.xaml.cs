using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using System.Diagnostics;
using System.Threading;
using System.Windows.Shell;
using System.Globalization;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TaskShedHandler taskShedHandler = new TaskShedHandler();
        RegEditHandler RegEditHandler = new RegEditHandler();
        Updater updater = new Updater();
        static bool themeSettingDark = false;
        static bool is1903 = false;

        public static bool ThemeSettingDark { get => themeSettingDark; set => themeSettingDark = value; }
        public static bool Is1903 { get => is1903; set => is1903 = value; }

        public MainWindow()
        {
            LanguageHelper();
            InitializeComponent();
            updater.CheckNewVersion();
            AddJumpList();

            //check OS-Version
            if (int.Parse(RegEditHandler.GetOSversion()).CompareTo(1900) > 0) Is1903 = true;
            //Properties.Settings.Default.Upgrade();

            GetCurTheme();
            DoesTaskExists();
            UiHandler();
        }

        private void LanguageHelper()
        {
            if (Properties.Settings.Default.Language.ToString() == "")
            {
                Properties.Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Properties.Settings.Default.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language);
        }

        private void GetCurTheme()
        {
            try
            {
                if (!Is1903 || !Properties.Settings.Default.SystemThemeChange.Equals(0))
                {
                    if (RegEditHandler.AppsUseLightTheme() == true) ThemeSettingDark = false;
                    else if (RegEditHandler.AppsUseLightTheme() == false) ThemeSettingDark = true;
                }
                else
                {
                    if (RegEditHandler.SystemUsesLightTheme() == true) ThemeSettingDark = false;
                    else if (RegEditHandler.AppsUseLightTheme() == false) ThemeSettingDark = true;
                }
            }
            catch
            {
                locationBlock.Text = Properties.Resources.msgThemeError;  //Warning: We couldn't read your current theme.
            }
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
            }
            else
            {
                autoCheckBox.IsChecked = false;
                AutoCheckBox_Unchecked(this, null);
            }
        }

        private void UiHandler()
        {
            int appTheme = Properties.Settings.Default.AppThemeChange;
            Console.WriteLine("appTheme Value: " + appTheme);
            if (appTheme == 0) AppComboBox.SelectedIndex = 0;
            if (appTheme == 1) AppComboBox.SelectedIndex = 1;
            if (appTheme == 2) AppComboBox.SelectedIndex = 2;

            int systemTheme = Properties.Settings.Default.SystemThemeChange;
            Console.WriteLine("SystemTheme Value: " + systemTheme);
            if (systemTheme == 0) SystemComboBox.SelectedIndex = 0;
            if (systemTheme == 1) SystemComboBox.SelectedIndex = 1;
            if (systemTheme == 2) SystemComboBox.SelectedIndex = 2;

            int edgeTheme = Properties.Settings.Default.EdgeThemeChange;
            Console.WriteLine("EdgeTheme Value: " + edgeTheme);
            if (edgeTheme == 0) EdgeComboBox.SelectedIndex = 0;
            if (edgeTheme == 1) EdgeComboBox.SelectedIndex = 1;
            if (edgeTheme == 2) EdgeComboBox.SelectedIndex = 2;

            if (!Is1903)
            {
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.cmb1903;
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.cmb1903;
            }

            if (Properties.Settings.Default.AccentColor)
            {
                AccentColorCheckBox.IsChecked = true;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //get values from TextBox
            int darkStart = int.Parse(darkStartBox.Text);
            int darkStartMinutes = int.Parse(DarkStartMinutesBox.Text);
            int lightStart = int.Parse(lightStartBox.Text);
            int lightStartMinutes = int.Parse(LightStartMinutesBox.Text);

            //check values from TextBox
            if(darkStart > 24)
            {
                darkStart = 24;
            }
            if(lightStart >= darkStart)
            {
                lightStart = darkStart - 2;
            }
            if (lightStart < 0)
            {
                lightStart = 1;
                darkStart = 3;
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
            LightStartMinutesBox.Text = Convert.ToString(lightStartMinutes);
            DarkStartMinutesBox.Text = Convert.ToString(darkStartMinutes);

            try
            {
                taskShedHandler.CreateTask(darkStart, darkStartMinutes, lightStart, lightStartMinutes);
                RegEditHandler.SwitchThemeBasedOnTime();
                RegEditHandler.AddAutoStart();

                //UI
                userFeedback.Text = Properties.Resources.msgChangesSaved;//changes were saved!
                applyButton.IsEnabled = false;
            }
            catch{
                userFeedback.Text = Properties.Resources.msgErrorOcc;//error occurred :(
            }
        }

        //textbox allow only numbers
        private void LightStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void DarkStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void LightStartMinutesBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void DarkStartMinutesBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //textbox block cut, copy & paste
        private void LightStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void DarkStartBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void LightStartMinutesBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void DarkStartMinutesBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        //open aboutWindow
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = GetWindow(this)
            };
            aboutWindow.Closed += new EventHandler(AboutWindow_Closed);
            aboutWindow.ShowDialog();
        }
        private void AboutWindow_Closed(object sender, EventArgs e)
        {
            if (Is1903)
            {
                SystemComboBox.IsEnabled = true;
                AccentColorCheckBox.IsEnabled = true;
            }
            if (!Is1903)
            {
                SystemComboBox.IsEnabled = false;
                AccentColorCheckBox.IsEnabled = false;
            }
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            //DebugSettings();
            //Application.Current.Shutdown();
            Thread.Sleep(2000);
            Process.GetCurrentProcess().Kill();
        }

        private void DebugSettings()
        {
            Console.WriteLine(Properties.Settings.Default.SystemThemeChange);
            Console.WriteLine(Properties.Settings.Default.AppThemeChange);
            Console.WriteLine(Properties.Settings.Default.EdgeThemeChange);
            Console.WriteLine(Properties.Settings.Default.LocationLatitude);
            Console.WriteLine(Properties.Settings.Default.LocationLongitude);
        }

        // set starttime based on user location
        private void LocationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GetLocation();
        }
        public async void GetLocation()
        {
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
                    darkStartBox.Text = sundate[2].ToString();
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
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        private void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            locationBlock.Text = "";
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
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            taskShedHandler.RemoveTask();
            RegEditHandler.RemoveAutoStart();

            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
        }

        //ComboBox
        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (AppComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.AppThemeChange = 0;
                if (ThemeSettingDark) RegEditHandler.AppTheme(0);
                if (!ThemeSettingDark) RegEditHandler.AppTheme(1);
            }
            if (AppComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.AppThemeChange = 1;
                RegEditHandler.AppTheme(1);
            }
            if (AppComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.AppThemeChange = 2;
                RegEditHandler.AppTheme(0);
            }
        }
        private void SystemComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.SystemThemeChange = 0;
                if (Properties.Settings.Default.AccentColor && !ThemeSettingDark)
                {
                    RegEditHandler.ColorPrevalence(0);
                    Thread.Sleep(500);
                    RegEditHandler.SystemTheme(1);
                }
                if(Properties.Settings.Default.AccentColor && ThemeSettingDark)
                {
                    RegEditHandler.SystemTheme(0);
                    Thread.Sleep(500);
                    RegEditHandler.ColorPrevalence(1);
                }
                AccentColorCheckBox.IsEnabled = true;
            }
            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.SystemThemeChange = 1;
                if (Properties.Settings.Default.AccentColor)
                {
                    RegEditHandler.ColorPrevalence(0);
                    Thread.Sleep(500);
                }
                RegEditHandler.SystemTheme(1);
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.IsChecked = false;
                GetCurTheme();
            }
            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                RegEditHandler.SystemTheme(0);
                if (Properties.Settings.Default.AccentColor)
                {
                    Thread.Sleep(500);
                    RegEditHandler.ColorPrevalence(1);
                }
                AccentColorCheckBox.IsEnabled = true;
                GetCurTheme();
            }
        }
        private void EdgeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (EdgeComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.EdgeThemeChange = 0;
                if (ThemeSettingDark) RegEditHandler.EdgeTheme(1);
                if (!ThemeSettingDark) RegEditHandler.EdgeTheme(0);
            }
            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.EdgeThemeChange = 1;
                RegEditHandler.EdgeTheme(0);
            }
            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.EdgeThemeChange = 2;
                RegEditHandler.EdgeTheme(1);
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AccentColor = true;
            if(themeSettingDark) RegEditHandler.ColorPrevalence(1);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AccentColor = false;
            RegEditHandler.ColorPrevalence(0);
        }
    }
}