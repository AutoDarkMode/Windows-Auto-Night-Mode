using System;
using System.Windows;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        { 
            //handle command line arguments
            if (e.Args.Length > 0)
            {
                string[] args = Environment.GetCommandLineArgs();
                foreach (var value in args)
                {
                    if(value == "/dark")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.ThemeToDark();
                    }
                    else if(value == "/light")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.ThemeToLight();
                    }else if(value == "/removeTask")
                    {
                        TaskShedHandler taskShedHandler = new TaskShedHandler();
                        taskShedHandler.RemoveTask();
                    }else if (value == "/location")
                    {
                        LocationHandler locationHandler = new LocationHandler();
                        await locationHandler.SetLocationSilent();
                    }else if (value == "/switch")
                    {
                        RegEditHandler regEditHandler = new RegEditHandler();
                        regEditHandler.SwitchThemeBasedOnTime();
                    }
                }
                Shutdown();
            }
            else
            {
                base.OnStartup(e);
            }
        }
    }
}
