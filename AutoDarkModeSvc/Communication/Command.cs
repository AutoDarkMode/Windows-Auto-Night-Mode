using System;
using System.IO;
using System.Linq;

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
        public const string Switch = "--switch";
        public const string Swap = "--swap";
        [Includable]
        public const string Light = "--light";
        [Includable]
        public const string Dark = "--dark";
        [Includable]
        public const string NoForce = "--no-force";
        [Includable]
        public const string CheckForUpdate = "--check-for-update";
        [Includable]
        public const string Update = "--update";
        public const string LocationAccess = "--location-access";
        [Includable]
        public const string AddAutostart = "--add-autostart";
        [Includable]
        public const string RemoveAutostart = "--remove-autoStart";
        [Includable]
        public const string Shutdown = "--exit";
        public const string TestError = "--test-error";
        public const string Alive = "--alive";
        [Includable]
        public const string DetectMonitors = "--detect-monitors";
        [Includable]
        public const string CleanMonitors = "--clean-monitors";
        public const string UpdateFailed = "--update-failed";
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IncludableAttribute : Attribute
    {
    }

    public static class StatusCode
    {
        public const string Available = "Available";
        public const string New = "New";
        public const string NoLocAccess = "NoLocAccess";
        public const string Err = "Err";
        public const string Ok = "Ok";
        public const string Timeout = "Timeout";
        public const string UnsupportedOperation = "UnsupportedOperation";
        public const string No = "No";
        public const string Disabled = "Disabled";
        public const string InProgress = "InProgress";
    }

    public class ApiResponse
    {
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return StatusCode != null && StatusCode.Length > 0
                ? Message != null && Message.Length > 0
                ? Details != null && Details.Length > 0
                ? $"{StatusCode}\n{Message}\n{Details}"
                : $"{StatusCode}\n{Message}"
                : $"{StatusCode}"
                : "";
        }

        public static ApiResponse FromString(string response)
        {
            try
            {
                string[] responseSplit = response.Split("\n");
                return new ApiResponse
                {
                    StatusCode = responseSplit.ElementAtOrDefault(0),
                    Message = responseSplit.ElementAtOrDefault(1),
                    Details = responseSplit.ElementAtOrDefault(2),
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    StatusCode = Communication.StatusCode.Err,
                    Message = ex.Message
                };
            }
        }
    }
}