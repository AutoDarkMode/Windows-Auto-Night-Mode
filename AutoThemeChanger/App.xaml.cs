using System;
using System.Windows;

namespace AutoThemeChanger
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
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.SwitchThemeBasedOnTime();
                    }
                    else if (value == "/swap")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
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
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.ThemeToDark();
                    }
                    else if(value == "/light")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
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
                    else if(value == "/removeTask")
                    {
                        TaskShedHandler taskShedHandler = new TaskShedHandler();
                        taskShedHandler.RemoveTask();
                    }
                    else if (value == "/removeAutostart")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.RemoveAutoStart();
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
    }
}
