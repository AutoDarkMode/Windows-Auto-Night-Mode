#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
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
                TimedThemeState ts = new();

                if (State.PostponeManager.Get(Helper.PostponeItemSessionLock) == null)
                {
                    if (!notified && Helper.NowIsBetweenTimes(ts.NextSwitchTime.TimeOfDay, ts.CurrentSwitchTime.AddMinutes(2).TimeOfDay) && !notified)
                    {
                        ToastHandler.InvokeDelayAutoSwitchNotifyToast();
                        notified = true;
                    }
                    else if (notified && DateTime.Compare(DateTime.Now, ts.CurrentSwitchTime.AddMinutes(1)) > 0) notified = false;
                }
            }


            if (!State.PostponeManager.IsPostponed)
            {
                Task.Run(() =>
                {
                    ThemeManager.RequestSwitch(new(SwitchSource.TimeSwitchModule, Theme.Automatic));
                });
            }
        }

        public override void DisableHook()
        {
            base.DisableHook();
        }
    }
}
