using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers
{
    internal class WMIHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static ManagementEventWatcher CreateHKCURegistryValueMonitor(Action callback, string keyPath, string key)
        {
            string sidString = SID.ToString();
            string queryString = $"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = " +
                $"'{sidString}\\\\{keyPath}' AND ValueName='{key}'";
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
}
