using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AutoDarkModeSvc
{
    class TrayIcon : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();
        public TrayIcon()
        {
            MenuItem exitMenuItem = new MenuItem("Close", new EventHandler(Exit));
            MenuItem switchMenuItem = new MenuItem("Switch theme", new EventHandler(SwitchThemeNow));

            notifyIcon.Icon = Properties.Resources.AutoDarkModeIcon;
            notifyIcon.Text = "Auto Dark Mode";
            notifyIcon.MouseDown += new MouseEventHandler(OpenApp);
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { switchMenuItem, exitMenuItem });
            notifyIcon.Visible = true;
        }
        private void Exit(object sender, EventArgs e)
        {
            notifyIcon.Dispose();
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