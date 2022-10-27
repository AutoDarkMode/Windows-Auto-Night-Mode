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
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDarkModeApp.Handlers
{
    static class RegistryHandler
    {
        //get windows version number, like 1607 or 1903
        public static string GetOSversion()
        {
            var osVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return osVersion;
        }

        public static string GetUbr()
        {
            var ubr = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion", "UBR", null);
            return ubr != null ? ubr.ToString() : "0";
        }

        public static bool IsDWMPrevalence()
        {
            try
            {
                using RegistryKey key = GetDWMKey();
                var enabled = key.GetValue("ColorPrevalence").Equals(1);
                return enabled;
            }
            catch
            {
                return true;
            }           
        }

        private static RegistryKey GetDWMKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", true);
            return registryKey;
        }
    }
}
