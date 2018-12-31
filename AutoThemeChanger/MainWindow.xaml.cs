using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using System.Diagnostics;
using System.Threading;

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
        static bool themeSettingDark;
        bool is1903 = false;

        public static bool ThemeSettingDark { get => themeSettingDark; set => themeSettingDark = value; }

        public MainWindow()
        {
            InitializeComponent();
            updater.CheckNewVersion();

            //checkOSVersion
            if (int.Parse(RegEditHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            //Get current theme and set bool
            if (!is1903)
            {
                if (RegEditHandler.AppsUseLightTheme() == true) ThemeSettingDark = false;
                else if (RegEditHandler.AppsUseLightTheme() == false) ThemeSettingDark = true;
            }
            else
            {
                if (RegEditHandler.SystemUsesLightTheme() == true) ThemeSettingDark = false;
                else if (RegEditHandler.AppsUseLightTheme() == false) ThemeSettingDark = true;
            }

            //check if task already exists
            if (taskShedHandler.CheckExistingClass().Equals(1))
            {
                autoCheckBox.IsChecked = true;
                darkStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("dark"));
                lightStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("light"));
            }else if (taskShedHandler.CheckExistingClass().Equals(2))
            {
                autoCheckBox.IsChecked = true;
                locationCheckBox.IsChecked = true;
            }
            else
            {
                autoCheckBox.IsChecked = false;
                AutoCheckBox_Unchecked(this, null);
            }
            UiHandlerComboBox();
        }

        private void UiHandlerComboBox()
        {
            //Properties.Settings.Default.Upgrade();

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

            if (!is1903)
            {
                SystemComboBox.IsEnabled = false;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //get values from TextBox
            int darkStart = int.Parse(darkStartBox.Text);
            int lightStart = int.Parse(lightStartBox.Text);

            //check values from TextBox
            if(darkStart > 24)
            {
                darkStart = 24;
                darkStartBox.Text = Convert.ToString(darkStart);
            }
            if(lightStart >= darkStart)
            {
                lightStart = darkStart - 1;
                lightStartBox.Text = Convert.ToString(lightStart);
            }
            if (lightStart < 0)
            {
                lightStart = 23;
                lightStartBox.Text = Convert.ToString(lightStart);
            }

            try
            {
                taskShedHandler.CreateTask(darkStart, lightStart);
                RegEditHandler.SwitchThemeBasedOnTime();

                //UI
                userFeedback.Text = "changes were saved!";
                applyButton.IsEnabled = false;
            }
            catch{
                userFeedback.Text = "error occurred :(";
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

        //open aboutWindow
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        //applicatin close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            DebugSettings();
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

        // set start time based on user location
        private void LocationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GetLocation();
        }
        public async void GetLocation()
        {
            locationBlock.Text = "Searching your location...";
            LocationHandler locationHandler = new LocationHandler();

            var accesStatus = await Geolocator.RequestAccessAsync();
            switch (accesStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    //locate user + get sunrise & sunset times
                    locationBlock.Text = "City: " + await locationHandler.GetCityName();
                    int[] sundate = await locationHandler.CalculateSunTime(false);

                    //apply settings & change UI
                    lightStartBox.Text = sundate[0].ToString();
                    darkStartBox.Text = sundate[1].ToString();
                    lightStartBox.IsEnabled = false;
                    darkStartBox.IsEnabled = false;
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
            locationBlock.Text = "The App needs permission to location";
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        private void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            lightStartBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            locationBlock.Text = "";
            userFeedback.Text = "Click on apply to save changes";
            taskShedHandler.RemoveLocationTask();
        }

        // automatic theme switch checkbox
        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            locationCheckBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            lightStartBox.IsEnabled = true;
            userFeedback.Text = "Click on apply to save changes";
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            taskShedHandler.RemoveTask();

            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            userFeedback.Text = "Activate the checkbox to enable automatic theme switching";
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
                if (ThemeSettingDark) RegEditHandler.SystemTheme(0);
                if (!ThemeSettingDark) RegEditHandler.SystemTheme(1);
            }
            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.SystemThemeChange = 1;
                RegEditHandler.SystemTheme(1);
            }
            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                RegEditHandler.SystemTheme(0);
            }
        }
        private void EdgeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (EdgeComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.EdgeThemeChange = 0;
                if (ThemeSettingDark) RegEditHandler.EdgeTheme(0);
                if (!ThemeSettingDark) RegEditHandler.EdgeTheme(1);
            }
            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.EdgeThemeChange = 1;
                RegEditHandler.EdgeTheme(1);
            }
            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.EdgeThemeChange = 2;
                RegEditHandler.EdgeTheme(0);
            }
        }

        private void DebugModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SystemComboBox.IsEnabled = true;
        }

        private void DebugModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SystemComboBox.IsEnabled = false;
        }
    }
}