using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

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
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.DoubleClick += ShowMainWindow;

            // Control Icon Path - .could be more dynamic
            notifyIcon.Icon = new Icon("../../AutoDarkModeIcon.ico");
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

            // Get text from Resources accoring to the languages
            notifyIcon.ContextMenuStrip.Items.Add("MainWindow...").Click += ShowMainWindow;
            notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += ExitApplication;
            notifyIcon.ShowBalloonTip(1000, "Auto Dark Mode", "Application still running...", System.Windows.Forms.ToolTipIcon.Info);

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
