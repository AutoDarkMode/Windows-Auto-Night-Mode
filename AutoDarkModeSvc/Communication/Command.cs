using System;
using System.IO;

namespace AutoDarkModeSvc.Communication
{
    public static class Address
    {
        public const string DefaultPipeName = "WindowsAutoDarkMode";
        public const string DefaultPipeResponse = "_response";
        public const string DefaultPipeCommand = "_command";
        public const string DefaultPort = "54345";
    }
    public static class Command
    {
        [Includable]
        public const string Switch = "/switch";
        public const string Swap = "/swap";
        [Includable]
        public const string Light = "/light";
        [Includable]
        public const string Dark = "/dark";
        [Includable]
        public const string NoForce = "/noForce";
        [Includable]
        public const string Update = "/update";
        public const string LocationAccess = "/locationAccess";
        [Includable]
        public const string AddAutostart = "/addAutostart";
        [Includable]
        public const string RemoveAutostart = "/removeAutoStart";
        [Includable]
        public const string Shutdown = "/exit";
        public const string TestError = "/testError";
        public const string Alive = "/alive";
        [Includable]
        public const string DetectMonitors = "/detectMonitors";
        [Includable]
        public const string CleanMonitors = "/cleanMonitors";
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal class IncludableAttribute : Attribute
    {
    }

    public static class Response
    {
        public const string Available = "Available";
        public const string New = "New";
        public const string NoLocAccess = "NoLocAccess";
        public const string Err = "Err";
        public const string Ok = "Ok";
        public const string Timeout = "Timeout";
    }
}