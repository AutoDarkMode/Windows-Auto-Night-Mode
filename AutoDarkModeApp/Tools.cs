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
    }
}
