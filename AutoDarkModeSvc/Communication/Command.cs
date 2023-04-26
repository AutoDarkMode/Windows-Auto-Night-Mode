#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.IO;
using System.Linq;

namespace AutoDarkModeSvc.Communication
{
    public static class Address
    {
        public const string PipePrefix = "admpipe";
        public const string PipeResponse = "_response";
        public static string PipeRequest { get; } = $"_request_{Environment.UserName.ToLower()}";
        public const string DefaultPort = "54345";
    }
    public static class Command
    {
        /// <summary>
        /// Invokes a theme switch based on time. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string RequestSwitch = "--switch";

        /// <summary>
        /// Basically useless currently, needs rework
        /// </summary>
        [Includable]
        public const string Swap = "--swap";

        /// <summary>
        /// Requests Auto Dark Mode to switch to the light theme and pauses 
        /// switching once. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string Light = "--light";

        /// <summary>
        /// Requests Auto Dark Mode to switch to the dark theme. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string Dark = "--dark";

        /// <summary>
        /// Forces light theme and sets the GlobalState force flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string ForceLight = "--force-light";

        /// <summary>
        /// Forces dark theme and sets the GlobalState force flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string ForceDark = "--force-dark";

        /// <summary>
        /// Resets the GlobalState force theme flag. Only returns an ApiResponse with StatusCode.Ok or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string NoForce = "--no-force";


        /// <summary>
        /// Delays the theme switch by a user set amount passed as additional parameter in minnutes 
        /// Returns an APIResponse with StatusCode.Ok, StatusCode.Err if parameter is invalid or a StatusCode.Timeout
        /// </summary>
        [Includable]
        public const string DelayBy = "--delay-by";

        /// <summary>
        /// Toggles skipping the next pending theme switch off or on
        /// Returns an APIResponse with StatusCode.Ok or a StatusCode.Timeout 
        /// and a message with true or false to indicate whether skipping is enabled or disabled
        /// </summary>
        [Includable]
        public const string ToggleSkipNext = "--toggle-skip-next";

        [Includable]
        public const string ClearPostponeQueue = "--clear-postpone-queue";

        [Includable]
        public const string GetPostponeStatus = "--get-postpone-status";

        /// <summary>
        /// Returns the internal theme that ADM is currently maintaining
        /// </summary>
        [Includable]
        public const string GetRequestedTheme = "--get-requested-theme";

        /// <summary>
        /// Returns the current system colorization color (accent color)
        /// Returns a hex string of the last parsed colorization color and the currently requested theme as details
        /// </summary>
        [Includable]
        public const string GetCurrentColorization = "--get-colorization";

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

        [Includable]
        public const string CheckForDowngradeNotify = "--check-for-downgrade-notify";

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
        public const string GetAutostartState = "--get-autostart-state";

        [Includable]
        public const string ValidateAutostart = "--validate-autostart";

        [Includable]
        public const string Shutdown = "--exit";

        [Includable]
        public const string Restart = "--restart";

        [Includable]
        public const string Alive = "--alive";

        [Includable]
        public const string DetectMonitors = "--detect-monitors";

        [Includable]
        public const string CleanMonitors = "--clean-monitors";

        public const string UpdateFailed = "--update-failed";

        public const string GetLearnedThemeNames = "--get-learned-theme-names";

        [Includable]
        public const string Test = "--test";

        [Includable]
        public const string Test2 = "--test2";

        public const string TestError = "--test-error";

        public const string TestNotifications = "--test-notifications";

        public const string TestNotifications2 = "--test-notifications2";
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class IncludableAttribute : Attribute
    {
    }

    public static class StatusCode
    {
        public static string Available { get; } = "Available";
        public static string New { get; } = "New";
        public static string Downgrade { get; } = "Downgrade";
        public static string NoLocAccess { get; } = "NoLocAccess";
        public static string Err { get; } = "Err";
        public static string Ok { get; } = "Ok";
        public static string Timeout { get; } = "Timeout";
        public static string UnsupportedOperation { get; } = "UnsupportedOperation";
        public static string No { get; } = "No";
        public static string Disabled { get; } = "Disabled";
        public static string InProgress { get; } = "InProgress";
        public static string AutostartTask { get; } = "AutostartTask";
        public static string AutostartRegistryEntry { get; } = "AutostartRegistryEntry";
        public static string Modified { get; } = "Modified";
    }

    public class ApiResponse
    {
        public const string separator = "\nAdmApiDataRow=";
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