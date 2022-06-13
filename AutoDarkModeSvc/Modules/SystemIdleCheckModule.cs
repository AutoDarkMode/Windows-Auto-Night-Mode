using AutoDarkModeConfig;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Timers;
using System;
using System.Runtime.InteropServices;


namespace AutoDarkModeSvc.Modules
{
    public class SystemIdleCheckModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public override string TimerAffinity => TimerName.Main;
        private GlobalState State { get; } = GlobalState.Instance();
        private AdmConfigBuilder builder { get; } = AdmConfigBuilder.Instance();

        public SystemIdleCheckModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration) { }

        public override void Fire()
        {
            LASTINPUTINFO lastinputStruct = new();
            lastinputStruct.cbSize = (uint)Marshal.SizeOf(lastinputStruct);
            GetLastInputInfo(ref lastinputStruct);

            DateTime lastInputTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastinputStruct.dwTime));
            if (lastInputTime <= DateTime.Now.AddMinutes(-builder.Config.IdleChecker.Threshold))
            {
                Logger.Info($"allow theme switch, system idle since {lastInputTime}, which is longer than {builder.Config.IdleChecker.Threshold} minute(s)");
                State.PostponeManager.Remove(Name);
            }
            else if (State.PostponeManager.Add(Name))
            {
                Logger.Info("postponing theme switch due to system idle timer");
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            State.PostponeManager.Remove(Name);
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }
    }

}
