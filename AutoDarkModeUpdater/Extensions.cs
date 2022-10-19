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
        public static readonly string ExecutionPathSvc = GetExecutionPathService();
        public static readonly string ExecutionPathApp = GetExecutionPathApp();
        public static readonly string ExecutionPathShell = GetExecutionPathShell();
        public static readonly string ExecutionDirUpdater = GetExecutionDirUpdater();

        private static string GetExecutionPathService()
        {
            var assemblyLocation = System.AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionPathApp()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeApp.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionPathShell()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeShell.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }

        private static string GetUpdateDataDir()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            assemblyLocation = Directory.GetParent(assemblyLocation).FullName;
            var dataPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "UpdateData");
            return dataPath;
        }

        private static string GetExecutionDirUpdater()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            return assemblyLocation.TrimEnd('\\');
        }

    }


}
