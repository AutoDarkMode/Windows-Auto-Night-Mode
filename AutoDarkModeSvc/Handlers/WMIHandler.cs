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
using System.Management;
using System.Security.Principal;

namespace AutoDarkModeSvc.Handlers;

internal class WMIHandler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static ManagementEventWatcher CreateHKCURegistryValueMonitor(Action callback, string keyPath, string key)
    {
        string sidString = SID.ToString();
        string queryString = $"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{sidString}\\\\{keyPath}' AND ValueName='{key}'";
        WqlEventQuery query = new WqlEventQuery(queryString);
        ManagementEventWatcher autostartWatcher = new(query);
        autostartWatcher.EventArrived += new EventArrivedEventHandler((s, e) => callback());
        return autostartWatcher;
    }

    private static SecurityIdentifier SID
    {
        get
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.User;
        }
    }
}
