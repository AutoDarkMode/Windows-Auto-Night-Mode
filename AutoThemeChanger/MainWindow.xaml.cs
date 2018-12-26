using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;

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
        bool is1903 = false;

        public MainWindow()
        {
            InitializeComponent();
            updater.CheckNewVersion();

            //checkOSVersion
            if (RegEditHandler.GetOSversion().Equals("1903")) is1903 = true;

            //check if task already exists
            if (taskShedHandler.CheckExistingClass().Equals(1))
            {
                autoRadio.IsChecked = true;
                darkStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("dark"));
                lightStartBox.Text = Convert.ToString(taskShedHandler.GetRunTime("light"));
                UiHandler();
            }else if (taskShedHandler.CheckExistingClass().Equals(2))
            {
                autoRadio.IsChecked = true;
                locationCheckBox.IsChecked = true;
                UiHandler();
            }
            else
            {
                //check which value the registry key has
                if (RegEditHandler.AppsUseLightTheme() == true) lightRadio.IsChecked = true;
                else if (RegEditHandler.AppsUseLightTheme() == false) darkRadio.IsChecked = true;
                UiHandler();
            }
        }

        private void LightRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.ThemeToLight();
            taskShedHandler.RemoveTask();
            UiHandler();
        }

        private void DarkRadio_Click(object sender, RoutedEventArgs e)
        {
            RegEditHandler.ThemeToDark();
            taskShedHandler.RemoveTask();
            UiHandler();
        }

        private void UiHandler()
        {
            if(autoRadio.IsChecked.Value == true)
            {
                locationCheckBox.IsEnabled = true;
                applyButton.IsEnabled = true;
                darkStartBox.IsEnabled = true;
                lightStartBox.IsEnabled = true;
                userFeedback.Text = "click on apply to save changes";
            }
            else if(autoRadio.IsChecked.Value == false)
            {
                locationCheckBox.IsEnabled = false;
                locationCheckBox.IsChecked = false;
                applyButton.IsEnabled = false;
                darkStartBox.IsEnabled = false;
                lightStartBox.IsEnabled = false;
                userFeedback.Text = "Choose change automatic to enable auto switch";
            }
        }

        private void AutoRadio_Click(object sender, RoutedEventArgs e)
        {
            UiHandler();
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

                //fit current theme to the time
                var time = DateTime.Now.Hour;
                if (time <= lightStart || time >= darkStart) RegEditHandler.ThemeToDark();
                else if (time >= lightStart || time <= darkStart) RegEditHandler.ThemeToLight();

                //UI
                userFeedback.Text = "changes were saved!";
            }
            catch{
                userFeedback.Text = "error occurred :(";
            }
        }

        //textbox allow only numbers
        private void LightStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void DarkStartBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
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
                    int[] sundate = await locationHandler.CalculateSunTime();

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
            TaskShedHandler taskShedHandler = new TaskShedHandler();
            taskShedHandler.RemoveLocationTask();
        }
    }
}