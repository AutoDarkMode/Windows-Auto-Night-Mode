using System;
using System.Windows;
using System.Windows.Shell;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Navigation;
using AutoDarkModeApp.Properties;
using AutoDarkModeSvc.Communication;
using AutoDarkModeApp.Pages;
using ModernWpf.Media.Animation;
using AdmProperties = AutoDarkModeLib.Properties;


namespace AutoDarkModeApp
{
    public partial class MainWindowMwpf
    {

        public MainWindowMwpf()
        {
            DataContext = this;

            Trace.WriteLine("--------- AppStart");

            //set current UI language
            LanguageHelper();

            InitializeComponent();
        }

        private void Window_OnSourceInitialized(object sender, EventArgs e)
        {
            if (Settings.Default.Width != 0 || Settings.Default.Height != 0)
            {
                Top = Settings.Default.Top;
                Left = Settings.Default.Left;
                Height = Settings.Default.Height;
                Width = Settings.Default.Width;
            }
            if (Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LanguageHelper();
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
                sysFormat = sysFormat[..sysFormat.IndexOf(":")];
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    Settings.Default.AlterTime = true;
                }
            }
            catch
            {

            }
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            //NetMQConfig.Cleanup();
            Settings.Default.Save();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill(); //needs kill if user uses location service
        }



        private void Window_Closing(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Settings.Default.Top = RestoreBounds.Top;
                Settings.Default.Left = RestoreBounds.Left;
                Settings.Default.Height = RestoreBounds.Height;
                Settings.Default.Width = RestoreBounds.Width;
                Settings.Default.Maximized = true;
            }
            else
            {
                Settings.Default.Top = Top;
                Settings.Default.Left = Left;
                Settings.Default.Height = Height;
                Settings.Default.Width = Width;
                Settings.Default.Maximized = false;
            }

            Settings.Default.Save();
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
                        FrameNavbar.Navigate(typeof(PagePersonalization), null, new EntranceNavigationTransitionInfo());
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
