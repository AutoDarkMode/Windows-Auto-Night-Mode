using System;
using System.Timers;

namespace AutoDarkModeSvc
{
    class BackgroundTask
    {
        private Service Svc;
        public BackgroundTask(Service svc)
        {
            Svc = svc;
            Timer timer = new Timer
            {
                Interval = 15000,
                AutoReset = false,
                Enabled = true
            };
            timer.Elapsed += TimerThemeSwitch;
            Console.WriteLine(DateTime.Now.TimeOfDay + " Timer started");
        }
        private void TimerThemeSwitch(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine(DateTime.Now.TimeOfDay + " Timer elapsed");
            Timer t = source as Timer;
            Svc.Stop();
        }
    }
}
