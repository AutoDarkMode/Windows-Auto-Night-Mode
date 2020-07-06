using System.IO;

namespace AutoDarkModeApp
{
    class Command
    {
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
        public const string AddLocationTask = "/addLocationTask";
        public const string RemoveLocationTask = "/removeLocationTask";
        public const string PipeClientTest = "/pipeclienttest";
        public const string UpdateConfig = "/updateConfig";
        public const string SystemThemeDark = "/systemThemeDark";
        public const string Shutdown = "/exit";
        public const string TestError = "/testError";
        public const string Alive = "/alive";

        //return types
        public const string Available = "Available";
        public const string New = "New";
        public const string NoLocAccess = "NoLocAccess";
        public const string Err = "Err";
        public const string Ok = "Ok";
        public const string Response = "Response";
    }
}
