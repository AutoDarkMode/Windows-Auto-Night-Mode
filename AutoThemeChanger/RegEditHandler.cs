using Microsoft.Win32;

namespace AutoThemeChanger
{
    class RegEditHandler
    {
        public void ThemeToDark()
        {
            if(Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(0);
            if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(0);
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(0);
        }

        public void ThemeToLight()
        {
            if (Properties.Settings.Default.AppThemeChange.Equals(0)) AppTheme(1);
            if (Properties.Settings.Default.SystemThemeChange.Equals(0)) SystemTheme(1);
            if (Properties.Settings.Default.EdgeThemeChange.Equals(0)) EdgeTheme(1);
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
            GetEdgeKey().SetValue("Theme", theme, RegistryValueKind.DWord);
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
    }
}
