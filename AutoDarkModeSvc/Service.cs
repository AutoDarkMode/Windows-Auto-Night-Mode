using System;
using System.Diagnostics;
using System.Windows.Forms;
using AutoDarkModeSvc.Communication;
using AutoDarkModeSvc.Modules;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc
{
    class Service : ApplicationContext
    {
        NotifyIcon NotifyIcon { get; }
        ModuleTimer ManhThai { get; }
        PipeService PipeSvc { get;  }
        public Service(int timerMillis)
        { 
            NotifyIcon = new NotifyIcon();
            InitTray();
            PipeSvc = new PipeService();
            PipeSvc.Start();
            ManhThai = new ModuleTimer(timerMillis);
            ManhThai.RegisterModule(new TimeSwitchModule("TimeSwitch"));
            ManhThai.Start();
        }

        private void InitTray()
        {
            MenuItem exitMenuItem = new MenuItem("Close", new EventHandler(Exit));
            MenuItem switchMenuItem = new MenuItem("Switch theme", new EventHandler(SwitchThemeNow));

            NotifyIcon.Icon = Properties.Resources.AutoDarkModeIcon;
            NotifyIcon.Text = "Auto Dark Mode";
            NotifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            NotifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { switchMenuItem, exitMenuItem });
            NotifyIcon.Visible = true;
        }

        private void Exit(object sender, EventArgs e)
        {
            PipeSvc.Stop();
            NotifyIcon.Dispose();
            ManhThai.Dispose();
            Application.Exit();
        }
        private void SwitchThemeNow(object sender, EventArgs e)
        {
            Console.WriteLine("Switch Theme");
        }
        private void OpenApp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine("Start App");
                Process.Start(@"AutoDarkModeApp.exe");
            }
        }
    }
}