using Microsoft.Win32;

namespace AutoThemeChanger
{
    class RegEditHandler
    {
        public void ThemeToDark()
        {
            GetKey().SetValue("AppsUseLightTheme", "0", RegistryValueKind.DWord);
        }

        public void ThemeToLight()
        {
            GetKey().SetValue("AppsUseLightTheme", "1", RegistryValueKind.DWord);
        }

        public bool AppsUseLightTheme()
        {
            var keyValue = GetKey().GetValue("AppsUseLightTheme");
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
    }
}
