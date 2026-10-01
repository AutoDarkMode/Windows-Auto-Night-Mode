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
using System.Linq;

namespace AutoDarkModeSvc.Communication;

public static class Address
{
    /// <summary>
    /// Prefix used for named pipes in ADM communication
    /// </summary>
    public const string PipePrefix = "admpipe";

    /// <summary>
    /// Suffix for response pipes
    /// </summary>
    public const string PipeResponse = "_response";

    /// <summary>
    /// Gets the request pipe name for the current user
    /// </summary>
    public static string PipeRequest { get; } = $"_request_{Environment.UserName.ToLower()}";

    /// <summary>
    /// Default port for ADM network communication
    /// </summary>
    public const string DefaultPort = "54345";
}

public static class Command
{
    #region Theme Switching Commands

    /// <summary>
    /// Invokes a theme switch based on time. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string RequestSwitch = "--switch";

    /// <summary>
    /// Requests Auto Dark Mode to switch to the light theme and pauses 
    /// switching once. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string Light = "--light";

    /// <summary>
    /// Requests Auto Dark Mode to switch to the dark theme. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string Dark = "--dark";

    /// <summary>
    /// Basically useless currently, needs rework
    /// </summary>
    [Includable]
    public const string Swap = "--swap";

    #endregion

    #region Force Theme Commands

    /// <summary>
    /// Forces light theme and sets the <see cref="GlobalState"/> force flag. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string ForceLight = "--force-light";

    /// <summary>
    /// Forces dark theme and sets the <see cref="GlobalState"/> force flag. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string ForceDark = "--force-dark";

    /// <summary>
    /// Resets the <see cref="GlobalState"/> force theme flag. Only returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string NoForce = "--no-force";

    #endregion

    #region Theme Switch Control Commands

    /// <summary>
    /// Delays the theme switch by a user set amount passed as additional parameter in minutes
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/>, <see cref="StatusCode.Err"/> if parameter is invalid or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string DelayBy = "--delay-by";

    /// <summary>
    /// Toggles skipping the next pending theme switch off or on
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/> 
    /// and a message with true or false to indicate whether skipping is enabled or disabled
    /// </summary>
    [Includable]
    public const string ToggleSkipNext = "--toggle-skip-next";

    /// <summary>
    /// Clears the postpone queue
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string ClearPostponeQueue = "--clear-postpone-queue";

    /// <summary>
    /// Gets the postpone status
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Timeout"/>
    /// </summary>
    [Includable]
    public const string GetPostponeStatus = "--get-postpone-status";

    #endregion

    #region Query/Status Commands

    /// <summary>
    /// Returns the internal theme that ADM is currently maintaining
    /// Returns an <see cref="ApiResponse"/> with the requested theme
    /// </summary>
    [Includable]
    public const string GetRequestedTheme = "--get-requested-theme";

    /// <summary>
    /// Returns the current system colorization color (accent color)
    /// Returns a hex string of the last parsed colorization color and the currently requested theme as details
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Err"/>
    /// </summary>
    [Includable]
    public const string GetCurrentColorization = "--get-colorization";

    #endregion

    #region Update Commands

    /// <summary>
    /// Checks for updates silently
    /// <returns>
    /// <see cref="ApiResponse"/> with <see cref="StatusCode.New"/> if an update is available, <br/>
    /// <see cref="StatusCode.Ok"/> if no update is available <br/>
    /// <see cref="StatusCode.Err"/> if an error has occurred. <br/>
    /// Message carries the current version string <br/>
    /// Details carries a yaml serialized UpdateInfo object
    /// </returns>
    /// </summary>
    [Includable]
    public const string CheckForUpdate = "--check-for-update";

    /// <summary>
    /// Checks for updates loudly (shows toast message)
    /// <returns>
    /// <see cref="ApiResponse"/> object as string with <see cref="StatusCode.New"/> if an update is available, <br/>
    /// <see cref="StatusCode.Ok"/> if no update is available <br/>
    /// <see cref="StatusCode.Err"/> if an error has occurred. <br/>
    /// <see cref="StatusCode.UnsupportedOperation"/> if ADM has been installed in all users mode <br/>
    /// <see cref="StatusCode.Disabled"/> if a manual update is required
    /// Message carries the current version string <br/>
    /// Details carries a yaml serialized UpdateInfo object
    /// </returns>
    /// </summary>
    [Includable]
    public const string CheckForUpdateNotify = "--check-for-update-notify";

    /// <summary>
    /// Checks for downgrade and notifies the user
    /// Returns an <see cref="ApiResponse"/> or notifies the user about a downgrade
    /// </summary>
    [Includable]
    public const string CheckForDowngradeNotify = "--check-for-downgrade-notify";

    /// <summary>
    /// Invokes an update. This requires that <see cref="CheckForUpdate"/> has been run beforehand
    /// <returns>
    /// <see cref="ApiResponse"/> object as string with <see cref="StatusCode.New"/> if an update can be performed, <br/>
    /// <see cref="StatusCode.UnsupportedOperation"/> if ADM has been installed in all users mode <br/>
    /// <see cref="StatusCode.Disabled"/> if a manual update is required
    /// Returns any other status code if the update was not possible.
    /// </returns>
    /// </summary>
    [Includable]
    public const string Update = "--update";

    #endregion

    #region Location Services Commands

    /// <summary>
    /// Checks if location access is available <br/>
    /// <returns>
    /// <see cref="ApiResponse"/> object as string with
    /// <see cref="StatusCode.Ok"/> if everything is well
    /// <see cref="StatusCode.Err"/> if an error has occurred, and a message if it is a regkey or task scheduler issue
    /// </returns>
    /// </summary>
    public const string LocationAccess = "--location-access";

    /// <summary>
    /// Checks if the geolocator is currently updating <br/>
    /// <returns>
    /// <see cref="ApiResponse"/> object as string with
    /// <see cref="StatusCode.Ok"/> if the locator is not updating
    /// <see cref="StatusCode.InProgress"/> if the geolocator is currently updating
    /// </returns>
    /// </summary>
    [Includable]
    public const string GeolocatorIsUpdating = "--geolocator-is-updating";

    #endregion

    #region Autostart Commands

    /// <summary>
    /// Adds Auto Dark Mode to autostart
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Err"/>
    /// </summary>
    [Includable]
    public const string AddAutostart = "--add-autostart";

    /// <summary>
    /// Removes Auto Dark Mode from autostart
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Err"/>
    /// </summary>
    [Includable]
    public const string RemoveAutostart = "--remove-autostart";

    /// <summary>
    /// Gets the autostart state
    /// Returns an <see cref="ApiResponse"/> with the current autostart state
    /// </summary>
    [Includable]
    public const string GetAutostartState = "--get-autostart-state";

    /// <summary>
    /// Validates the autostart configuration
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/> or <see cref="StatusCode.Err"/>
    /// </summary>
    [Includable]
    public const string ValidateAutostart = "--validate-autostart";

    #endregion

    #region Service Control Commands

    /// <summary>
    /// Shuts down the Auto Dark Mode service
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/>
    /// </summary>
    [Includable]
    public const string Shutdown = "--exit";

    /// <summary>
    /// Restarts the Auto Dark Mode service
    /// Returns an <see cref="ApiResponse"/> with <see cref="StatusCode.Ok"/>
    /// </summary>
    [Includable]
    public const string Restart = "--restart";

    #endregion
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