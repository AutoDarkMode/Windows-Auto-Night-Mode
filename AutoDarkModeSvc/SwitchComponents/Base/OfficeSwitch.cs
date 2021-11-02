using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class OfficeSwitch : BaseComponent<OfficeSwitchSettings>
    {
        private Theme currentComponentTheme = Theme.Unknown;
        private int ChoosenLightTheme = 0;

        public override bool ThemeHandlerCompatibility { get; } = true;

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if(currentComponentTheme == Theme.Unknown)
            {
                return true;
            }
            if (Settings.Component.Mode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.LightOnly && currentComponentTheme != Theme.Light)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch && currentComponentTheme != newTheme)
            {
                return true;
            }
            else if (ChoosenLightTheme != Settings.Component.LightTheme)
            {
                return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            try
            {
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    OfficeTheme(Settings.Component.DarkTheme);
                    currentComponentTheme = Theme.Dark;

                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    OfficeTheme(Settings.Component.LightTheme);
                    currentComponentTheme = Theme.Light;
                    ChoosenLightTheme = Settings.Component.LightTheme;
                }
                else
                {
                    if (newTheme == Theme.Dark)
                    {
                        OfficeTheme(Settings.Component.DarkTheme);
                    }
                    else
                    {
                        OfficeTheme(Settings.Component.LightTheme);
                    }
                    currentComponentTheme = newTheme;
                    ChoosenLightTheme = Settings.Component.LightTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set office theme");
            }
            Logger.Info($"update info - previous: {oldTheme}, now: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}");
        }

        /// <summary>
        /// Changes the office theme
        /// </summary>
        /// <param name="themeValue">0 = colorful, 3 = grey, 4 = black, 5 = white</param>
        private void OfficeTheme(byte themeValue)
        {
            string officeCommonKey = @"Software\Microsoft\Office\16.0\Common";

            //edit first registry key
            using RegistryKey commonKey = Registry.CurrentUser.OpenSubKey(officeCommonKey, true);
            commonKey.SetValue("UI Theme", themeValue);

            //search for the second key and then change it
            using RegistryKey identityKey = Registry.CurrentUser.OpenSubKey(officeCommonKey + @"\Roaming\Identities\", true);

            string msaSubkey = @"\Settings\1186\{00000000-0000-0000-0000-000000000000}\";
            string anonymousSubKey = msaSubkey + @"\PendingChanges";

            foreach (var v in identityKey.GetSubKeyNames())
            {
                //registry key for users logged in with msa
                if (!v.Equals("Anonymous"))
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + msaSubkey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + msaSubkey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
                //registry key for users without msa
                else
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + anonymousSubKey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + anonymousSubKey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
            }
        }
    }
}
