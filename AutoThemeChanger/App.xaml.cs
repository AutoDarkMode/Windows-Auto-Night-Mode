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
                mainWin.Show();
            }
        }
    }
}
