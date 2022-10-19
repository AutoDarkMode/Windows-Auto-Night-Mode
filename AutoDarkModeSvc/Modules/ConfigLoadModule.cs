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
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Timers;

namespace AutoDarkModeSvc.Modules
{
    [Obsolete]
    class ConfigLoadModule : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private AdmConfigBuilder ConfigBuilder { get;  }
        public override string TimerAffinity { get; } = TimerName.IO;
         
        /// <summary>
        /// Instantiates a new ConfigUpdateModule.
        /// This module reloads the configuration file periodically
        /// </summary>
        /// <param name="name">unique name of the module</param>
        public ConfigLoadModule(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            ConfigBuilder = AdmConfigBuilder.Instance();
        }
        public override void Fire()
        {
            Task.Run(() =>
            {
                try
                {
                    ConfigBuilder.Load();
                    ConfigBuilder.LoadLocationData();
                    Logger.Debug("updated configuration file");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not read config file");
                }
            });
        }

    }
}
