using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeUpdater
{
    class Extensions
    {
        public static readonly string ExecutionDir = GetExecutionDir();
        public static readonly string UpdateDataDir = GetUpdateDataDir();
        public static readonly string ExecutionPath = GetExecutionPath();
        public static readonly string ExecutionDirUpdater = GetExecutionDirUpdater();
        public const string UpdaterDirName = "Updater";

        private static string GetExecutionPath()
        {
            var assemblyLocation = System.AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = System.AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }

        private static string GetUpdateDataDir()
        {
            var assemblyLocation = System.AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var dataPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "UpdateData");
            return dataPath;
        }

        private static string GetExecutionDirUpdater()
        {
            var assemblyLocation = System.AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath, UpdaterDirName);
        }

    }


}
