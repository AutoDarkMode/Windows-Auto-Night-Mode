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
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Monitors.ConfigUpdateEvents
{
    public abstract class ConfigUpdateEvent<T> : IConfigUpdateEvent<T>
    {
        protected NLog.Logger Logger { get; private set; }
        protected T oldConfig;
        protected T newConfig;
        protected GlobalState State { get; } = GlobalState.Instance();
        public ConfigUpdateEvent()
        {
            Logger = NLog.LogManager.GetLogger(GetType().ToString());
        }
        public void OnConfigUpdate(object sender, T newConfig)
        {
            if (sender is T oldConfig)
            {
                this.oldConfig = oldConfig;
                this.newConfig = newConfig;
                ChangeEvent();
            }
        }
        protected abstract void ChangeEvent();
    }
}
