using System;
using System.Windows;
using System.Windows.Shell;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Navigation;
using AutoDarkModeApp.Properties;
using NetMQ;
using AutoDarkModeSvc.Communication;
using AutoDarkModeApp.Pages;
using System.Windows.Media;
using ModernWpf.Media.Animation;

namespace AutoDarkModeApp
{
    public partial class MainWindow
    {

        public MainWindow()
        {
            DataContext = this;

            Trace.WriteLine("--------- AppStart");

            //set current UI language
            LanguageHelper();

            InitializeComponent();
            
            //only run at first startup
            if (Settings.Default.FirstRun)
            {
                //check if system uses 12 hour clock
                SystemTimeFormat();

                //create jump list entries
                AddJumpList();

                //finished first startup code
                Settings.Default.FirstRun = false; 
            }

            //run if user changed language in previous session
            if (Settings.Default.LanguageChanged)
            {
                AddJumpList();
                Settings.Default.LanguageChanged = false;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LanguageHelper();

            //TODO: REENABLE UPDATER OR FIND BETTER SOLUTION
            /*
            try
            {
                Updater updater = new(false);
                updater.CheckNewVersion(); //check github xaml file for a higher version number than installed
                if (updater.UpdateAvailable())
                {
                    updater.MessageBoxHandler(this);
                }
            }
            catch
            {

            }
            */
        }

        //region time and language
        private static void LanguageHelper()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.Language.ToString()))
            {
                try
                {
                    Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
                }
                catch
                {
                    Settings.Default.Language = CultureInfo.CreateSpecificCulture("en").ToString();
                }
                
            }
            var langCode = new CultureInfo(Settings.Default.Language);
            CultureInfo.CurrentUICulture = langCode;
            CultureInfo.CurrentCulture = langCode;
            CultureInfo.DefaultThreadCurrentUICulture = langCode;
            CultureInfo.DefaultThreadCurrentCulture = langCode;
        }

        private static void SystemTimeFormat()
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
        private static void AddJumpList()
        {
            JumpTask darkJumpTask = new()
            {
                //Dark theme
                Title = Properties.Resources.lblDarkTheme,
                Arguments = Command.Dark,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };
            JumpTask lightJumpTask = new()
            {
                //Light theme
                Title = Properties.Resources.lblLightTheme,
                Arguments = Command.Light,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };
            JumpTask resetJumpTask = new()
            {
                //Reset
                Title = Properties.Resources.lblReset,
                Arguments = Command.NoForce,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };

            JumpList jumpList = new();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.JumpItems.Add(resetJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

            JumpList.SetJumpList(Application.Current, jumpList);
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            NetMQConfig.Cleanup();
            Settings.Default.Save();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill(); //needs kill if user uses location service
        }

        /// <summary>
        /// Navbar / NavigationView
        /// </summary>
        
        //change displayed page based on selection
        private void NavBar_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();

                switch (navItemTag)
                {
                    case "time":
                        FrameNavbar.Navigate(typeof(PageTime), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "modes":
                        FrameNavbar.Navigate(typeof(PageSwitchModes), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "apps":
                        FrameNavbar.Navigate(typeof(PageApps), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "wallpaper":
                        FrameNavbar.Navigate(typeof(PageWallpaper), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "settings":
                        FrameNavbar.Navigate(typeof(PageSettings), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "donation":
                        FrameNavbar.Navigate(typeof(PageDonation), null, new EntranceNavigationTransitionInfo());
                        break;
                    case "about":
                        FrameNavbar.Navigate(typeof(PageAbout), null, new EntranceNavigationTransitionInfo());
                        break;
                }
                ScrollViewerNavbar.ScrollToTop();
            }
        }

        //startup page
        private void NavBar_Loaded(object sender, RoutedEventArgs e)
        {
            NavBar.SelectedItem = NavBar.MenuItems[1];
        }

        //Block back and forward on the frame
        private void FrameNavbar_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Forward | e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }
    }
}