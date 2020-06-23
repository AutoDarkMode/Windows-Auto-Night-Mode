using System;
using System.Windows;
using System.Windows.Shell;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Navigation;
using AutoThemeChanger.Properties;

namespace AutoThemeChanger
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            Console.WriteLine("--------- AppStart");
            LanguageHelper(); //set current UI language
            InitializeComponent();

            //only run at first startup
            if (Settings.Default.FirstRun)
            {
                SystemTimeFormat(); //check if system uses 12 hour clock
                AddJumpList(); //create jump list entries
                Settings.Default.FirstRun = false; 
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ButtonNavarTime_Click(this, null); //select and display the main page
            DonationScreen();
            Updater updater = new Updater();
            updater.CheckNewVersion(); //check github xaml file for a higher version number
        }

        private void DonationScreen()
        {
            //generate random number between 1 and 100. If the number is 50, show donation msgbox
            Random rdmnumber = new Random();
            int generatedNumber = rdmnumber.Next(1, 100);
            if (generatedNumber == 50)
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.donationDescription, Properties.Resources.donationTitle, "smiley", "yesno")
                {
                    Owner = GetWindow(this)
                };
                msgBox.ShowDialog();
                var result = msgBox.DialogResult;
                if (result == true)
                {
                    Process.Start("https://www.paypal.me/arminosaj");
                }
            }
        }

        //region time and language
        private void LanguageHelper()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.Language.ToString()))
            {
                Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            CultureInfo.CurrentUICulture = new CultureInfo(Settings.Default.Language, true);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Default.Language, true);
        }

        private void SystemTimeFormat()
        {
            try
            {
                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                sysFormat = sysFormat.Substring(0, sysFormat.IndexOf(":"));
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    Settings.Default.AlterTime = true;
                }
            }
            catch
            {

            }
        }

        //jump list
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

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill(); //needs kill if user uses location service
        }

        //navigation bar
        private void ButtonNavarTime_Click(object sender, RoutedEventArgs e)
        {
            FrameNavbar.Navigate(new Uri(@"/Pages/PageTime.xaml", UriKind.Relative));
            NavbarRectangle.Margin = new Thickness(0, 45, 0, 0);
        }

        private void ButtonNavbarApps_Click(object sender, RoutedEventArgs e)
        {
            FrameNavbar.Navigate(new Uri(@"/Pages/PageApps.xaml", UriKind.Relative));
            NavbarRectangle.Margin = new Thickness(0,90,0,0);
        }

        private void ButtonNavbarWallpaper_Click(object sender, RoutedEventArgs e)
        {
            FrameNavbar.Navigate(new Uri(@"/Pages/PageWallpaper.xaml", UriKind.Relative));
            NavbarRectangle.Margin = new Thickness(0, 135, 0, 0);
        }

        private void ButtonNavbarSettings_Click(object sender, RoutedEventArgs e)
        {
            FrameNavbar.Navigate(new Uri(@"/Pages/PageSettings.xaml", UriKind.Relative));
            NavbarRectangle.Margin = new Thickness(0, 180, 0, 0);
        }

        private void ButtonNavbarAbout_Click(object sender, RoutedEventArgs e)
        {
            FrameNavbar.Navigate(new Uri(@"/Pages/PageAbout.xaml", UriKind.Relative));
            NavbarRectangle.Margin = new Thickness(0, 490, 0, 0);
        }

        //frame
        private void FrameNavbar_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Forward | e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }
    }
}