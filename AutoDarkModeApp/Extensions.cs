using System;
using System.Collections.Generic;
using System.IO;

namespace AutoDarkModeApp
{
    static class Extensions
    {
        public static readonly string ExecutionPath = GetExecutionPath();
        public static readonly string ExecutionDir = GetExecutionDir();
        private static string GetExecutionPath()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableName = Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(assemblyLocation) + ".exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }

        private static string GetExecutionDir()
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return executablePath;
        }
    }
}
