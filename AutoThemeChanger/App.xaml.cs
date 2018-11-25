using System;
using System.Windows;

namespace AutoThemeChanger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //handle command line arguments
            if (e.Args.Length > 0)
            {
                TaskShedHandler taskShedHandler = new TaskShedHandler();
                RegEditHandler regEditHandler = new RegEditHandler();

                string[] args = Environment.GetCommandLineArgs();
                foreach (var value in args)
                {
                    if(value == "/dark")
                    {
                        regEditHandler.ThemeToDark();
                    }
                    else if(value == "/light")
                    {
                        regEditHandler.ThemeToLight();
                    }else if(value == "/removeTask")
                    {
                        taskShedHandler.RemoveTask();
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
