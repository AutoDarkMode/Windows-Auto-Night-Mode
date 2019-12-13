using AutoThemeChanger.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;

namespace AutoThemeChanger
{
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private bool isExit = false;
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            //handle command line arguments
            if (e.Args.Length > 0)
            {
                string[] args = Environment.GetCommandLineArgs();
                foreach (var value in args)
                {
                    if (value == "/switch")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.SwitchThemeBasedOnTime();
                    }
                    else if (value == "/swap")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        if (regEditHandler.AppsUseLightTheme())
                        {
                            regEditHandler.ThemeToDark();
                        }
                        else
                        {
                            regEditHandler.ThemeToLight();
                        }
                    }
                    else if (value == "/dark")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.ThemeToDark();
                    }
                    else if (value == "/light")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.ThemeToLight();
                    }
                    else if (value == "/update")
                    {
                        Updater updater = new Updater();
                        updater.CheckNewVersion();
                    }
                    else if (value == "/location")
                    {
                        LocationHandler locationHandler = new LocationHandler();
                        await locationHandler.SetLocationSilent();
                    }
                    else if (value == "/removeTask")
                    {
                        TaskShedHandler taskShedHandler = new TaskShedHandler();
                        taskShedHandler.RemoveTask();
                    }
                    else if (value == "/removeAutostart")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.RemoveAutoStart();
                    }
                }
                Shutdown();
            }
            else
            {
                MainWindow mainWin = new MainWindow();
                MainWindow.Closing += MainWindow_Closing;
                mainWin.Show();
            }
        }

        private void InitiateNotifyIcon()
        {
            string notifyIconExitText = AutoThemeChanger.Properties.Resources.notifyIconExitContentItemText;
            string notifyBalloonTipTitle = AutoThemeChanger.Properties.Resources.Title;
            string notifyBalloonTipText = AutoThemeChanger.Properties.Resources.notifyBalloonTipText;
            string notifySwapThemeText = AutoThemeChanger.Properties.Resources.notifyIconSwapThemeContentItemText;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.DoubleClick += ShowMainWindow;

            // accessing to the icon with path could be more dynamic
            notifyIcon.Icon = new Icon("../../AutoDarkModeIcon.ico");
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add(notifyBalloonTipTitle).Click += ShowMainWindow;
            notifyIcon.ContextMenuStrip.Items.Add(notifySwapThemeText).Click += SwapTheme;
            notifyIcon.ContextMenuStrip.Items.Add(notifyIconExitText).Click += ExitApplication;
            notifyIcon.ShowBalloonTip(1000, notifyBalloonTipTitle, notifyBalloonTipText, System.Windows.Forms.ToolTipIcon.Info);
        }

        private void SwapTheme(object sender, EventArgs e)
        {
            //
            // It just swaps the current theme but should behave according to the custom preferences of the user
            // like edge theme, app theme and system theme
            //

            RegeditHandler regEditHandler = new RegeditHandler();
            if (regEditHandler.AppsUseLightTheme())
            {
                regEditHandler.ThemeToDark();
            }
            else
            {
                regEditHandler.ThemeToLight();
            }
        }

        private void DisposeNotifyIcon()
        {
            if (notifyIcon != null) 
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }

        private void ShowMainWindow(object sender, EventArgs e)
        {
            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Show();
            }
            DisposeNotifyIcon();
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            isExit = true;
            MainWindow.Close();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!isExit)
            {
                InitiateNotifyIcon();
                e.Cancel = true;
                MainWindow.Hide();
            }
            else
            {
                DisposeNotifyIcon();
            }
        }
    }
}
