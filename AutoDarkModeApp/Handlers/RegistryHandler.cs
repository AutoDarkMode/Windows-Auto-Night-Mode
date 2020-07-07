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
    }
}
