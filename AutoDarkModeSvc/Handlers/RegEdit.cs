using Microsoft.Win32;
using System;
using System.Threading;
using AutoDarkModeApp.Config;
using System.IO;
using AutoDarkModeApp;

namespace AutoDarkModeSvc.Handlers
{
    class RegEdit
    {
        private AutoDarkModeConfigBuilder Properties{ get; set; }
        public void SwitchThemeBasedOnTime()
        {
            Properties = AutoDarkModeConfigBuilder.GetInstance();
            TaskSchd task = new TaskSchd();
            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;
            var lightStart = task.GetRunTime("light");
            var darkStart = task.GetRunTime("dark");

            if (hour < lightStart[0] || hour >= darkStart[0])
            {
                if (hour == darkStart[0])
                {
                    if (minute < darkStart[1])
                    {
                        ThemeToLight();
                    }
                    if (minute >= darkStart[1])
                    {
                        ThemeToDark();
                    }
                }
                else
                {
                    ThemeToDark();
                }
            }
            else if (hour >= lightStart[0] || hour < darkStart[0])
            {
                if (hour == lightStart[0])
                {
                    if (minute < lightStart[1])
                    {
                        ThemeToDark();
                    }
                    if (minute >= lightStart[1])
                    {
                        ThemeToLight();
                    }
                }
                else
                {
                    ThemeToLight();
                }
            }
        }

        public void ThemeToDark()
        {
            if (Properties.Config.AppsTheme.Equals(0)) AppsTheme(0);
            if (Properties.Config.SystemTheme.Equals(0)) SystemTheme(0);
            if (Properties.Config.EdgeTheme.Equals(0)) EdgeTheme(1);

            if (!Properties.Config.Wallpaper.Disabled)
            {
                DeskBG.SetBackground(Properties.Config.Wallpaper.LightThemeWallpapers);
            }

            if (Properties.Config.AccentColorTaskbar && Properties.Config.SystemTheme.Equals(0))
            {
                Thread.Sleep(200);
                ColorPrevalence(1);
            }
        }

        public void ThemeToLight()
        {
            if (Properties.Config.AccentColorTaskbar && Properties.Config.SystemTheme.Equals(0))
            {
                ColorPrevalence(0);
                Thread.Sleep(200);
            }

            if (Properties.Config.AppsTheme.Equals(0)) AppsTheme(1);
            if (Properties.Config.SystemTheme.Equals(0)) SystemTheme(1);
            if (Properties.Config.EdgeTheme.Equals(0)) EdgeTheme(0);

            if (!Properties.Config.Wallpaper.Disabled)
            {
                DeskBG.SetBackground(Properties.Config.Wallpaper.LightThemeWallpapers);
            }
        }

        public void AppsTheme(int theme)
        {
            GetKey().SetValue("AppsUseLightTheme", theme, RegistryValueKind.DWord);
        }

        public void SystemTheme(int theme)
        {
            GetKey().SetValue("SystemUsesLightTheme", theme, RegistryValueKind.DWord);
        }

        public void EdgeTheme(int theme)
        {
            try
            {
                GetEdgeKey().SetValue("Theme", theme, RegistryValueKind.DWord);
            }
            catch
            {

            }
        }

        public void ColorPrevalence(int theme)
        {
            GetKey().SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }

        public bool AppsUseLightTheme()
        {
            var keyValue = GetKey().GetValue("AppsUseLightTheme");
            if ((int)keyValue == 1) return true;
            else return false;
        }

        public bool SystemUsesLightTheme()
        {
            var keyValue = GetKey().GetValue("SystemUsesLightTheme");
            if ((int)keyValue == 1) return true;
            else return false;
        }

        public string GetOSversion()
        {
            var osVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return osVersion;
        }

        private RegistryKey GetKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            return registryKey;
        }

        private RegistryKey GetEdgeKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\Main", true);
            return registryKey;
        }

        public void AddAutoStart()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            registryKey.SetValue("AutoDarkMode", '\u0022' + Tools.ExecutionDir + '\u0022' + @" /switch");
        }
        public void RemoveAutoStart()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            registryKey.DeleteValue("AutoDarkMode", false);
        }
    }
}