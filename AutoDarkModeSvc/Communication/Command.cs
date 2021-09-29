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
        /// <summary>
        /// Invokes a theme switch based on time. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string Switch = "--switch";

        /// <summary>
        /// Basically useless currently, needs rework
        /// </summary>
        public const string Swap = "--swap";

        /// <summary>
        /// Forces light theme and sets the GlobalState force flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string Light = "--light";

        /// <summary>
        /// Forces dark theme and sets the GlobalState force flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string Dark = "--dark";

        /// <summary>
        /// Resets the GlobalState force theme flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string NoForce = "--no-force";

        /// <summary>
        /// Checks for updates silently
        /// ApiResponse with StatusCode.New if an update is available, <br/>
        /// StatusCode.Ok if no update is available <br/>
        /// StatusCode.Err if an error has occurred. <br/>
        /// Message carries the current version string <br/>
        /// Details carries a yaml serialized UpdateInfo object</summary>
        [Includable]
        public const string CheckForUpdate = "--check-for-update";

        /// <summary>
        /// Checks for updates loudly (shows toast message)
        /// ApiResponse object as string with StatusCode.New if an update is available, <br/>
        /// StatusCode.Ok if no update is available <br/>
        /// StatusCode.Err if an error has occurred. <br/>
        /// StatusCode.UnsupportedOperation if ADM has been installed in all users mode <br/>
        /// StatusCode.Disabled if a manual update is required
        /// Message carries the current version string <br/>
        /// Details carries a yaml serialized UpdateInfo object</summary>
        [Includable]
        public const string CheckForUpdateNotify = "--check-for-update-notify";

        /// <summary>
        /// Invokes an update. this requires that CheckForUpdate has been run beforehand
        /// Returns an ApiResponse object as string with StatusCode.New if an update can be performed, <br/>
        /// StatusCode.UnsupportedOperation if ADM has been installed in all users mode <br/>
        /// StatusCode.Disabled if a manual update is required
        /// Returns any other status code if the update was not possible.
        /// </summary>
        [Includable]
        public const string Update = "--update";

        /// <summary>
        /// Checks if locationAccess is available <br/>
        /// Returns an ApiResponse object as string with
        /// StatusCode.Ok if everything is well
        /// Statuscode.Err if an error has occurred, and a message if it is a regkey or task scheduler issue
        /// </summary>
        public const string LocationAccess = "--location-access";

        /// <summary>
        /// Checks if the geolocator is currently updating <br/>
        /// Returns an ApiResponse object as string with
        /// StatusCode.Ok if the locator is not updating
        /// StatusCode.InProgress if the geolocator is currently updating
        /// </summary>
        [Includable]
        public const string GeolocatorIsUpdating = "--geolocator-is-updating";

        [Includable]
        public const string AddAutostart = "--add-autostart";

        [Includable]
        public const string RemoveAutostart = "--remove-autostart";

        [Includable]
        public const string Shutdown = "--exit";

        public const string TestError = "--test-error";

        public const string Alive = "--alive";

        [Includable]
        public const string DetectMonitors = "--detect-monitors";

        [Includable]
        public const string CleanMonitors = "--clean-monitors";

        [Includable]
        public const string UpdateFailed = "--update-failed";

        [Includable]
        public const string TestNotifications = "--test-notifications";
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
        private const string separator = "\nAdmApiDataRow=";
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return StatusCode != null && StatusCode.Length > 0
                ? Message != null && Message.Length > 0
                ? Details != null && Details.Length > 0
                ? $"{StatusCode}{separator}{Message}{separator}{Details}"
                : $"{StatusCode}{separator}{Message}"
                : $"{StatusCode}"
                : "";
        }

        public static ApiResponse FromString(string response)
        {
            try
            {
                string[] responseSplit = response.Split(separator);
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