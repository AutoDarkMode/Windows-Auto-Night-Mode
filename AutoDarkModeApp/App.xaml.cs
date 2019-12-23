using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Config;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
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
                    else if (value == "/pipeclienttest")
                    {
                        PipeClient pc = new PipeClient("WindowsAutoDarkMode");
                        pc.SendMessage("Test");

                    }
                }
                Shutdown();
            }
            else
            {
                MainWindow mainWin = new MainWindow();
                mainWin.Show();
            }
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
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            MainWindow.Close();
        }
    }
}
