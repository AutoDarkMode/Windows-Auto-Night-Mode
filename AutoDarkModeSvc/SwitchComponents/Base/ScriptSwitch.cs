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
using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class ScriptSwitch : BaseComponent<ScriptSwitchSettings>
    {
        public override int PriorityToDark => 30;
        public override int PriorityToLight => 30;
        private Theme currentComponentTheme = Theme.Unknown;
        public override bool ThemeHandlerCompatibility { get; } = true;
        Task switchTask;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (currentComponentTheme != e.Theme)
            {
                return true;
            }
            return false;
        }

        protected override async void HandleSwitch(SwitchEventArgs e)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            switchTask = Task.Run(() =>
            {
                if (e.Theme == Theme.Light)
                {
                    Settings.Component.Scripts.ForEach(s =>
                    {
                        if (s.AllowedSources.Contains(SwitchSource.Any) || s.AllowedSources.Contains(e.Source))
                            ScriptHandler.Launch(s.Name, s.Command, s.ArgsLight, s.TimeoutMillis, s.WorkingDirectory);
                    });
                    currentComponentTheme = Theme.Light;
                }
                else
                {
                    Settings.Component.Scripts.ForEach(s =>
                    {
                        if (s.AllowedSources.Contains(SwitchSource.Any) || s.AllowedSources.Contains(e.Source))
                            ScriptHandler.Launch(s.Name, s.Command, s.ArgsDark, s.TimeoutMillis, s.WorkingDirectory);
                    });
                    currentComponentTheme = Theme.Dark;
                }
            });
            await switchTask;           
            Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), currentComponentTheme)}");
        }

        protected override void EnableHook()
        {
            currentComponentTheme = Theme.Unknown;
        }

        protected override void DisableHook()
        {
            currentComponentTheme = Theme.Unknown;
        }

        protected override void UpdateSettingsState()
        {
            if (!Settings.Component.Equals(SettingsBefore.Component))
            {
                currentComponentTheme = Theme.Unknown;
            }
        }

        /*
        protected override async void Callback()
        {
            if (switchTask != null)
            {
                await switchTask;
            }
            Logger.Debug("test callback wait");
        }
        */
    }
}
