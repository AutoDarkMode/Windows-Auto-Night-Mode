using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

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
                taskShedHandler taskShedHandler = new taskShedHandler();
                RegEditHandler regEditHandler = new RegEditHandler();

                string[] args = Environment.GetCommandLineArgs();
                foreach (var value in args)
                {
                    if(value == "/dark")
                    {
                        regEditHandler.themeToDark();
                    }
                    else if(value == "/light")
                    {
                        regEditHandler.themeToLight();
                    }else if(value == "/removeTask")
                    {
                        taskShedHandler.removeTask();
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
