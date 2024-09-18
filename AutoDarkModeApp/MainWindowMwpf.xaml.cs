#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Navigation;
using AutoDarkModeApp.Handlers;
using AutoDarkModeApp.Properties;
using AutoDarkModeApp.Pages;
using ModernWpf.Media.Animation;
using System.Windows.Media.Media3D;
using static AutoDarkModeApp.PInvoke.ParameterTypes;
using System.Windows.Interop;
using ModernWpf;
using System.Windows.Media;
using static AutoDarkModeApp.PInvoke;
using AutoDarkModeLib.IThemeManager2;
using AutoDarkModeLib;
using ModernWpf.Controls;
using System.Windows.Controls;
using ModernWpf.Controls.Primitives;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Threading;

namespace AutoDarkModeApp
{
    public partial class MainWindowMwpf : Window
    {
        private ResourceDictionary navbarDict;
        private ManagementEventWatcher DWMPrevalenceWatcher;
        private bool micaInitialized = false;
        private CancellationTokenSource refreshTitleBarCts = new();

        private static SecurityIdentifier SID
        {
            get
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return identity.User;
            }
        }

        public MainWindowMwpf()
        {
            DataContext = this;

            Trace.WriteLine("--------- AppStart");

            //set current UI language
            LanguageHelper();

            InitializeComponent();
            navbarDict = NavBar.Resources;

            try
            {
                string sidString = SID.ToString();
                string queryString = $"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = " +
                    $"'{sidString}\\\\" +
                    @"Software\\Microsoft\\Windows\\DWM' AND ValueName='ColorPrevalence'";
                WqlEventQuery query = new WqlEventQuery(queryString);
                DWMPrevalenceWatcher = new ManagementEventWatcher(query);
                DWMPrevalenceWatcher.EventArrived += new EventArrivedEventHandler((s, e) => Dispatcher.Invoke(RefreshWindowStyle));
                DWMPrevalenceWatcher.Start();
            }
            catch
            {
                // ignore, only causes minor UI instability if it fails
            }


            Loaded += OnLoaded;

            Architecture Arch = RuntimeInformation.ProcessArchitecture;
            if (Arch == Architecture.Arm64)
            {
                string title = "ARMto Dark Mode";
                WindowTitle.Text = title;
                Title = title;
            }
            else
            {
                string title = "Auto Dark Mode";
                WindowTitle.Text = title;
                Title = title;
            }
        }

        private void ConfigureMica()
        {
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.Win11_22H2)
            {
                Methods.SetWindowAttribute(
                    new WindowInteropHelper(this).Handle,
                    DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    2
                );
                RefreshFrame();
                RefreshWindowStyle();
                ThemeManager.Current.ActualApplicationThemeChanged += (_, _) => RefreshWindowStyle();
            }
            else
            {
                TopBarCloseButtonsForMica.Visibility = Visibility.Collapsed;
            }
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            ConfigureMica();
        }

        private void RefreshFrame()
        {
            IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            MARGINS margins = new MARGINS();
            margins.cxLeftWidth = -1;
            margins.cxRightWidth = -1;
            margins.cyTopHeight = -1;
            margins.cyBottomHeight = -1;

            Methods.ExtendFrame(mainWindowSrc.Handle, margins);
        }

        private void RefreshWindowStyle()
        {
            var isDark = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
            int flag = isDark ? 1 : 0;

            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            var newResources = new ResourceDictionary
            {
                ["NavigationViewTopPaneBackground"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                ["NavigationViewDefaultPaneBackground"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                ["NavigationViewExpandedPaneBackground"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
            };
            NavBar.Resources = newResources;

            if (!RegistryHandler.IsDWMPrevalence())
            {
                if (micaInitialized)
                {
                    CancellationToken token = refreshTitleBarCts.Token;
                    Task.Delay(3000, token).ContinueWith(t =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        Dispatcher.Invoke(() =>
                        {
                            TopBarCloseButtonsOpaque.Visibility = Visibility.Hidden;
                            TopBarHeader.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                            TopBarTitle.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                        });
                    });
                }
                else
                {
                    TopBarCloseButtonsOpaque.Visibility = Visibility.Hidden;
                    TopBarHeader.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    TopBarTitle.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    micaInitialized = true;
                }              
            }
            else
            {
                // allow full transparency except for close buttons
                if (!refreshTitleBarCts.IsCancellationRequested)
                {
                    refreshTitleBarCts.Cancel();
                }
                refreshTitleBarCts.Dispose();
                refreshTitleBarCts = new();
                TopBarCloseButtonsOpaque.Visibility = Visibility.Visible;
                TopBarHeader.SetResourceReference(BackgroundProperty, "AltHighClone");
                TopBarTitle.SetResourceReference(BackgroundProperty, "AltHighClone");      
            }


            Methods.SetWindowAttribute(
                new WindowInteropHelper(this).Handle,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                flag);
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
            DonationScreen();
        }

        private void DonationScreen()
        {
            //generate random number between 1 and 100. If the number is under or equals 2, show donation page
            Random rdmnumber = new Random();
            int generatedNumber = rdmnumber.Next(1, 100);
            Debug.WriteLine("Donation gamble number: " + generatedNumber);
            if (generatedNumber <= 2)
            {
                NavBar.SelectedItem = NavBar.FooterMenuItems[0];
            }
        }


        //region time and language
        private static void LanguageHelper()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.Language.ToString()))
            {
                try
                {
                    Settings.Default.Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToString();
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

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                DWMPrevalenceWatcher.Stop();
                DWMPrevalenceWatcher.Dispose();
            }
            catch { }

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
        private void NavBar_SelectionChanged(ModernWpf.Controls.NavigationView sender,
            ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                StateUpdateHandler.ClearAllEvents();

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
                    case "scripts":
                        FrameNavbar.Navigate(typeof(PageScripts), null, new EntranceNavigationTransitionInfo());
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

        private void HelpMenuItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            ProcessHandler.StartProcessByProcessInfo(
                "https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Troubleshooting");
        }
    }
}