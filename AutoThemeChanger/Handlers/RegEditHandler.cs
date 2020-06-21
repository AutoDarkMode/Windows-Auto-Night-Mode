using AutoThemeChanger.Properties;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows.Documents;
using Windows.System;
using WindowsInput;
using WindowsInput.Native;

namespace AutoThemeChanger
{
    class RegeditHandler
    {
        public void SwitchThemeBasedOnTime()
        {
            TaskSchHandler task = new TaskSchHandler();
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
            if (Properties.Settings.Default.ThemeSwitch)
            {
                ThemeHandler.ChangeTheme(Properties.Settings.Default.ThemeDark);
            }
            else
            {
                if (Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(0);
                if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(0);
                if (Properties.Settings.Default.WallpaperSwitch)
                {
                    WallpaperHandler.SetBackground(Properties.Settings.Default.WallpaperDark);
                }

                if (Properties.Settings.Default.AccentColor && Properties.Settings.Default.SystemThemeChange.Equals(0))
                {
                    Thread.Sleep(Properties.Settings.Default.AccentColorSwitchTime);
                    ColorPrevalence(1);
                }
            }
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(1);
            if (Properties.Settings.Default.OfficeThemeChange.Equals(0)) OfficeTheme(4);
            if (Properties.Settings.Default.ColourFilterKeystroke) ColourFilterKeySender(true);
        }

        public void ThemeToLight()
        {
            if (Properties.Settings.Default.ThemeSwitch)
            {
                ThemeHandler.ChangeTheme(Properties.Settings.Default.ThemeLight);
            }
            else
            {
                if (Properties.Settings.Default.AccentColor && Properties.Settings.Default.SystemThemeChange.Equals(0))
                {
                    ColorPrevalence(0);
                    Thread.Sleep(Properties.Settings.Default.AccentColorSwitchTime);
                }
                if (Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(1);
                if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(1);
                if (Properties.Settings.Default.WallpaperSwitch)
                {
                    WallpaperHandler.SetBackground(Properties.Settings.Default.WallpaperLight);
                }
            }
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(0);
            if (Properties.Settings.Default.OfficeThemeChange.Equals(0)) OfficeTheme(0);
            if (Properties.Settings.Default.ColourFilterKeystroke) ColourFilterKeySender(false);
        }

        public void ColourFilterKeySender(bool dark)
        {
            var filterKey = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\ColorFiltering", "Active", null);
            if (dark && filterKey.Equals(0) || !dark && filterKey.Equals(1))
            {
                //simulate key presses
                InputSimulator inputSimulator = new InputSimulator();
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LCONTROL);
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);
            }
        }
        public void ColourFilterSetup()
        {
            var filterType = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\ColorFiltering", true);
            //on clean installs this registry key doesn't exist, so we need to create it
            if(filterType == null)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\ColorFiltering", true);
                ColourFilterSetup(); //calling itself again prevents code doubling, because we need again to open the subkey
                return;
            }
            var currentValue = filterType.GetValue("Value", null);
            if (currentValue == null)
            {
                filterType.SetValue("Active", 0, RegistryValueKind.DWord);
            }

            //set filtertype to 0 for grayscale and activate hotkey functionality of windows
            filterType.SetValue("FilterType", 0, RegistryValueKind.DWord); // 0 = gray
            filterType.SetValue("HotkeyEnabled", 1, RegistryValueKind.DWord); //and we activate the hotkey as free bonus :)

            //and because we are nice, we also activate the colour filter depending on the current theme
            if (Settings.Default.AppThemeChange.Equals(0) && AppsUseLightTheme() || Settings.Default.SystemThemeChange.Equals(0) && SystemUsesLightTheme())
            {
                ColourFilterKeySender(false);
            }
            else if (Settings.Default.AppThemeChange.Equals(0) && !AppsUseLightTheme() || Settings.Default.SystemThemeChange.Equals(0) && !SystemUsesLightTheme())
            {
                ColourFilterKeySender(true);
            }
        }

        public void AppTheme(int theme)
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
            registryKey.SetValue("AutoDarkMode", '\u0022' + System.Reflection.Assembly.GetExecutingAssembly().Location + '\u0022' + @" /switch");
        }
        public void RemoveAutoStart()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            registryKey.DeleteValue("AutoDarkMode", false);
        }

        public void OfficeTheme(byte themeValue)
        {
            string themeRegKey = @"Software\Microsoft\Office\16.0\Common";

            //edit first registry key
            RegistryKey commonKey = Registry.CurrentUser.OpenSubKey(themeRegKey, true);
            commonKey.SetValue("UI Theme", themeValue);

            //search for the second key and then change it
            RegistryKey identityKey = Registry.CurrentUser.OpenSubKey(themeRegKey + @"\Roaming\Identities\", true);
            foreach (var v in identityKey.GetSubKeyNames())
            {
                try
                {
                    RegistryKey settingsKey = identityKey.OpenSubKey(v + @"\Settings\1186\{00000000-0000-0000-0000-000000000000}\", true);
                    settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                }
                catch
                {

                }
            }
        }
    }
}