using AutoThemeChanger.Properties;
using Microsoft.Win32;
using System;
using System.Threading;
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
            if (Settings.Default.ThemeSwitch)
            {
                ThemeHandler.ChangeTheme(Settings.Default.ThemeDark);
            }
            else
            {
                if (Settings.Default.AppThemeChange.Equals(0)) AppTheme(0);
                if (Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(0);
                if (Settings.Default.WallpaperSwitch)
                {
                    WallpaperHandler.SetBackground(Settings.Default.WallpaperDark);
                }

                if (Settings.Default.AccentColor && Settings.Default.SystemThemeChange.Equals(0))
                {
                    Thread.Sleep(Settings.Default.AccentColorSwitchTime);
                    ColorPrevalence(1);
                }
            }
            if (Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(1);
            if (Settings.Default.OfficeThemeChange.Equals(0)) OfficeTheme(4);
            if (Settings.Default.ColourFilterKeystroke) ColourFilterKeySender(true);
        }

        public void ThemeToLight()
        {
            if (Settings.Default.ThemeSwitch)
            {
                ThemeHandler.ChangeTheme(Settings.Default.ThemeLight);
            }
            else
            {
                if (Settings.Default.AccentColor && Settings.Default.SystemThemeChange.Equals(0))
                {
                    ColorPrevalence(0);
                    Thread.Sleep(Settings.Default.AccentColorSwitchTime);
                }
                if (Settings.Default.AppThemeChange.Equals(0)) AppTheme(1);
                if (Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(1);
                if (Settings.Default.WallpaperSwitch)
                {
                    WallpaperHandler.SetBackground(Settings.Default.WallpaperLight);
                }
            }
            if (Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(0);
            if (Settings.Default.OfficeThemeChange.Equals(0) & !Settings.Default.OfficeThemeChangeWhiteDesign) OfficeTheme(0);
            else if (Settings.Default.OfficeThemeChange.Equals(0) & Settings.Default.OfficeThemeChangeWhiteDesign) OfficeTheme(5);
            if (Settings.Default.ColourFilterKeystroke) ColourFilterKeySender(false);
        }

        //Colour filter grayscale feature
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
                filterType = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\ColorFiltering", true);
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

        //set AppUseLightTheme dword
        public void AppTheme(int theme)
        {
            GetKey().SetValue("AppsUseLightTheme", theme, RegistryValueKind.DWord);
        }
        //set SystemUsesLightTheme dword
        public void SystemTheme(int theme)
        {
            GetKey().SetValue("SystemUsesLightTheme", theme, RegistryValueKind.DWord);
        }
        //set Edge theme dword
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
        //set accent color for taskbar dword
        public void ColorPrevalence(int theme)
        {
            GetKey().SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }
        //get value of AppsUseLightTheme
        public bool AppsUseLightTheme()
        {
            var keyValue = GetKey().GetValue("AppsUseLightTheme");
            return ((int)keyValue == 1) ? true : false;
        }
        //get value of SystemUsesLightTheme
        public bool SystemUsesLightTheme()
        {
            var keyValue = GetKey().GetValue("SystemUsesLightTheme");
            return ((int)keyValue == 1) ? true : false;
        }
        //get windows version number, like 1607 or 1903
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
            string officeCommonKey = @"Software\Microsoft\Office\16.0\Common";

            //edit first registry key
            RegistryKey commonKey = Registry.CurrentUser.OpenSubKey(officeCommonKey, true);
            commonKey.SetValue("UI Theme", themeValue);

            //search for the second key and then change it
            RegistryKey identityKey = Registry.CurrentUser.OpenSubKey(officeCommonKey + @"\Roaming\Identities\", true);

            string msaSubkey = @"\Settings\1186\{00000000-0000-0000-0000-000000000000}\";
            string anonymousSubKey = msaSubkey + @"\PendingChanges";

            foreach (var v in identityKey.GetSubKeyNames())
            {
                //registry key for users logged in with msa
                if (!v.Equals("Anonymous"))
                {
                    try
                    {
                        RegistryKey settingsKey = identityKey.OpenSubKey(v + msaSubkey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        var createdSettingsKey = identityKey.CreateSubKey(v + msaSubkey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
                //registry key for users without msa
                else
                {
                    try
                    {
                        RegistryKey settingsKey = identityKey.OpenSubKey(v + anonymousSubKey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        var createdSettingsKey = identityKey.CreateSubKey(v + anonymousSubKey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
            }
        }
    }
}