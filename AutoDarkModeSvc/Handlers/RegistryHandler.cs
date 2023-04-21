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
using AutoDarkModeLib;
using AutoDarkModeSvc.Handlers.IThemeManager2;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace AutoDarkModeSvc.Handlers
{
    static class RegistryHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string GetUbr()
        {
            try
            {
                var ubr = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion", "UBR", null);
                return ubr != null ? ubr.ToString() : "0";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error while retrieving ubr, assuming none present");
            }
            return "0";
        }

        /// <summary>
        /// Switches system applications theme
        /// </summary>
        /// <param name="theme">0 for dark, 1 for light theme</param>
        public static void SetAppsTheme(int theme)
        {
            using var key = GetPersonalizeKey();
            key.SetValue("AppsUseLightTheme", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Switches the system theme
        /// </summary>
        /// <param name="theme"><0 for dark, 1 for light theme</param>
        public static void SetSystemTheme(int theme)
        {
            using RegistryKey key = GetPersonalizeKey();
            key.SetValue("SystemUsesLightTheme", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Sets the taskbar color prevalence
        /// </summary>
        /// <param name="theme">0 for disabled, 1 for enabled</param>
        public static void SetColorPrevalence(int theme)
        {
            using RegistryKey key = GetPersonalizeKey();
            key.SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Checks if color prevalence is enabled
        /// </summary>
        /// <returns>true if enabled; false otherwise</returns>
        public static bool IsColorPrevalence()
        {
            using RegistryKey key = GetPersonalizeKey();
            var enabled = key.GetValue("ColorPrevalence").Equals(1);
            return enabled;
        }

        public static void SetDWMPrevalence(int theme)
        {
            using RegistryKey key = GetDWMKey();
            key.SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }

        public static bool IsDWMPrevalence()
        {
            using RegistryKey key = GetDWMKey();
            var enabled = key.GetValue("ColorPrevalence").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Checks if system apps theme is light
        /// </summary>
        /// <returns>true if light; false if dark</returns>
        public static bool AppsUseLightTheme()
        {
            using RegistryKey key = GetPersonalizeKey();
            var enabled = key.GetValue("AppsUseLightTheme").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Checks if the system's theme is light
        /// </summary>
        /// <returns>true if light; false if dark</returns>
        public static bool SystemUsesLightTheme()
        {
            using RegistryKey key = GetPersonalizeKey();
            var enabled = key.GetValue("SystemUsesLightTheme").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Checks if the bluelight is enabled
        /// </summary>
        /// <returns>true if enabled; false otherwise</returns>
        public static bool IsNightLightEnabled()
        {
            using RegistryKey key = GetNightLightKey();
            var data = key.GetValue("Data");
            if (data is null)
                return false;
            var byteData = (byte[])data;
            return byteData.Length > 24 && byteData[23] == decimal.ToByte(0x10) && byteData[24] == decimal.ToByte(0x00);
        }

        public static string GetActiveThemePath()
        {
            // call first becaues it refreshes the regkey
            (bool isCustom, string activeThemeName) = Tm2Handler.GetActiveThemeName();
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes");
            string themePath = (string)key.GetValue("CurrentTheme") ?? new(Path.Combine(Helper.PathThemeFolder, "Custom.theme"));

            ThemeFile tempTheme = null;
            if (themePath.Length > 0)
            {
                tempTheme = new(themePath);
                tempTheme.Load();
            }
            else
            {
                Logger.Warn($"theme file path registry key empty, using custom theme, path: {themePath}");
                return themePath;
            }

            if (isCustom)
            {
                Logger.Debug($"current theme tracked by windows is custom theme, path: {themePath}");
            }
            else if (tempTheme.DisplayName.StartsWith("@%SystemRoot%\\System32\\themeui.dll"))
            {
                Logger.Debug($"current theme tracked by windows is default theme with localized name, path: {themePath}");
            }
            else if (tempTheme == null || tempTheme.DisplayName != activeThemeName)
            {
                // if the name of the retrieved theme doesn't match, we will just select the custom theme as fallback
                Logger.Debug($"expected name: {activeThemeName} different from display name: {tempTheme.DisplayName} with path: {themePath}");
                themePath = new(Path.Combine(Helper.PathThemeFolder, "Custom.theme"));
            }
            else
            {
                Logger.Debug($"current theme tracked by windows: {activeThemeName}, path: {themePath}");
            }
            return themePath;
        }

        /// <summary>
        /// Retrieves the operating system version
        /// </summary>
        /// <returns>operating system version string</returns>
        public static string GetOSversion()
        {
            var osVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return osVersion;
        }

        /// <summary>
        /// Gets the current user's personaliation registry key
        /// </summary>
        /// <returns>HKCU personalization RegistryKey</returns>
        private static RegistryKey GetPersonalizeKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            return registryKey;
        }

        private static RegistryKey GetDWMKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", true);
            return registryKey;
        }

        /// <summary>
        /// Gets the current user's bluelight registry key value
        /// </summary>
        /// <returns>HKCU bluelight RegistryKey value</returns>
        private static RegistryKey GetNightLightKey()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\default$windows.data.bluelightreduction.bluelightreductionstate\windows.data.bluelightreduction.bluelightreductionstate");
            return key;
        }


        /// <summary>
        /// Adds the application to Windows autostart
        /// </summary>
        public static bool AddAutoStart()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey.SetValue("AutoDarkMode", '\u0022' + Helper.ExecutionPath + '\u0022');
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not add service to autostart");
                return false;
            }

        }

        public static string GetAutostartPath()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                string admKey = (string)registryKey.GetValue("AutoDarkMode");
                return admKey;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not retrieve autostart entry:");
                return null;
            }
        }

        public static bool IsAutostartApproved()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true);
                byte[] admKey = (byte[])registryKey.GetValue("AutoDarkMode");
                if (admKey == null)
                {
                    return true;
                }
                if (admKey[0] == 2 || admKey[0] == 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not retrieve autostart startup approved entry:");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the application from Windows autostart. Exceptions handled
        /// </summary>
        public static bool RemoveAutoStart()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey.DeleteValue("AutoDarkMode", false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not remove AutoDarkModeSvc from autostart");
            }
            return false;
        }

        public static string GetCurrentWallpaperSource()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Desktop\General");
                return (string)registryKey.GetValue("WallpaperSource");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error getting wallpaper source path");
            }
            return "";
        }

        //Colour filter grayscale feature
        public static void ColorFilterKeySender(bool dark)
        {
            var filterKey = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\ColorFiltering", "Active", null);
            if ((dark && filterKey.Equals(0)) || (!dark && filterKey.Equals(1)))
            {
                //simulate key presses
                InputSimulator inputSimulator = new();
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LCONTROL);
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);
            }
        }
        public static bool IsColorFilterActive()
        {
            var filterKey = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\ColorFiltering", "Active", null);
            if (filterKey != null)
            {
                return filterKey.Equals(1);
            }
            else
            {
                return false;
            }
        }
        public static void ColorFilterSetup()
        {
            RegistryKey filterType = null;
            try
            {
                filterType = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\ColorFiltering", true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error instantiating color filtering key:");
            }
            //on clean installs this registry key doesn't exist, so we need to create it
            if (filterType == null)
            {
                Logger.Warn("color filter key does not exist, creating");
                filterType = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\ColorFiltering", true);
            }
            var currentValue = filterType.GetValue("Active", null);
            if (currentValue == null) filterType.SetValue("Active", 0, RegistryValueKind.DWord);

            var currentType = filterType.GetValue("FilterType", null);
            if (currentType == null) filterType.SetValue("FilterType", 0, RegistryValueKind.DWord); // 0 = gray

            filterType.SetValue("HotkeyEnabled", 1, RegistryValueKind.DWord); //and we activate the hotkey as free bonus :)
            filterType.Dispose();
        }
    }
}
