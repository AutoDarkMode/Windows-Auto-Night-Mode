using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handler;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AutoDarkModeApp
{
    public partial class App : Application
    {
        private readonly AutoDarkModeConfigBuilder autoDarkModeConfigBuilder = AutoDarkModeConfigBuilder.Instance();
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isClassicMode;
            try
            {
                isClassicMode = autoDarkModeConfigBuilder.Config.ClassicMode;
            }
            catch (Exception)
            {
                isClassicMode = false;
            }

            List<string> args;
            if (e.Args.Length > 0)
            {
                args = new List<string>(e.Args);
            }
            else
            {
                args = new List<string>();
            }


            using Process svc = new Process();
            if (e.Args.Length == 0 || e.Args.Length > 0 && e.Args[0] != "/debug")
            {
                svc.StartInfo.UseShellExecute = false;
                svc.StartInfo.FileName = Path.Combine(Tools.ExecutionDir, "AutoDarkModeSvc.exe");
                svc.StartInfo.CreateNoWindow = true;
                svc.Start();
            }
            else
            {
                args.Remove("/debug");
            }

            //handle command line arguments
            if (args.Count > 0)
            {
                ICommandClient commandClient = new ZeroMQClient(Tools.DefaultPort);
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
                        TaskSchdHandler.RemoveTask();
                    }
                    else if (value == "/removeAutostart")
                    {
                        RegeditHandler regEditHandler = new RegeditHandler();
                        regEditHandler.RemoveAutoStart();
                    }
                    else if (value == "/pipeclienttest")
                    {
                        //ICommandClient pc = new PipeClient(Tools.DefaultPipeName);
                        commandClient.SendMessage(Tools.TestError);
                    }

                    if (isClassicMode) commandClient.SendMessage(Tools.Shutdown);
                    NetMQConfig.Cleanup();
                    Shutdown();
                }
            }
            else
            {
                MainWindow mainWin = new MainWindow();
                mainWin.Show();
            }
        }
    }
}
