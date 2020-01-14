using System.IO;

namespace AutoDarkModeApp
{
    class Tools
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

        public const string DefaultPipeName = "WindowsAutoDarkMode";
        public const string DefaultPipeResponse = "_response";
        public const string DefaultPipeCommand = "_command";
        public const string DefaultPort = "54345";

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
        public const string SystemThemeDark = "/systemThemeDark";
        public const string Shutdown = "/exit";

        //return types
        public const string Available = "Available";
        public const string Err = "Err";
        public const string Ok = "Ok";
        public const string TestError = "/testError";
    }
}
