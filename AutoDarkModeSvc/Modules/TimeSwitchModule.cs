using System;
using AutoDarkModeLib;
using System.Threading.Tasks;
using AutoDarkModeSvc.Timers;
using System.Diagnostics.CodeAnalysis;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.Modules
{
    class TimeSwitchModule : AutoDarkModeModule
    {
        public override string TimerAffinity { get; } = TimerName.Main;
        private AdmConfigBuilder Builder { get; }
        private GlobalState State { get; } = GlobalState.Instance();
        private bool notified = false;

        /// <summary>
        /// Instantiates a new TimeSwitchModule.
        /// This module switches themes based on system time and sunrise/sunset
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public TimeSwitchModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            Builder = AdmConfigBuilder.Instance();
        }

        public override void Fire()
        {
            if (Builder.Config.AutoSwitchNotify.Enabled)
            {
                ThemeState ts = new();
                if (Helper.NowIsBetweenTimes(ts.NextSwitchTime.AddMinutes(-Math.Abs(-2)).TimeOfDay, ts.NextSwitchTime.TimeOfDay))
                {
                    if (!notified)
                    {
                        State.PostponeManager.Add(new(Helper.DelayGracePeriodItemName, DateTime.Now.AddMinutes(Builder.Config.AutoSwitchNotify.GracePeriodMinutes), SkipType.Unspecified));
                        ToastHandler.InvokeDelayAutoSwitchNotificationToast();
                        notified = true;
                    }
                }
                else if (notified)
                {
                    notified = false;
                }
            }

            if (!State.PostponeManager.IsPostponed)
            {
                Task.Run(() =>
                {
                    ThemeManager.RequestSwitch(new(SwitchSource.TimeSwitchModule));
                });
            }
        }

        public override void DisableHook()
        {
            base.DisableHook();
        }
    }
}
