using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoDarkModeApp
{
    class Tools
    {
        public static readonly string ExecutionDir = GetExeuctionDir();

        private static string GetExeuctionDir()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableName = Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(assemblyLocation) + ".exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        public const string Switch = "/switch";
        public const string Swap = "/swap";
        public const string Light = "/light";
        public const string Dark = "/dark";
        public const string Update = "/update";
        public const string Location = "/location";
        public const string CreateTask = "/createTask";
        public const string RemoveTask = "/removeTask";
        public const string AddAutostart = "/addAutostart";
        public const string RemoveAutostart = "/removeAutoStart";
        public const string PipeClientTest = "/pipeclienttest";
        public const string UpdateConfig = "/updateConfig";
    }
}
