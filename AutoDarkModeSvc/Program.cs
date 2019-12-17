using System;
using System.Windows.Forms;

namespace AutoDarkModeSvc
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            int timerMillis = 0;
            if (args.Length != 0)
            {
                Int32.TryParse(args[0], out timerMillis);
            }
            timerMillis = (timerMillis == 0) ? 10000 : timerMillis;
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Service(timerMillis));
        }
    }
}
