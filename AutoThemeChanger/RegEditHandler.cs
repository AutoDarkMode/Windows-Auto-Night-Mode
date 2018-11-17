using Microsoft.Win32;

namespace AutoThemeChanger
{
    class RegEditHandler
    {
        public void themeToDark()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true); ;
            registryKey.SetValue("AppsUseLightTheme", "0", RegistryValueKind.DWord);
        }

        public void themeToLight()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true); ;
            registryKey.SetValue("AppsUseLightTheme", "1", RegistryValueKind.DWord);
        }

        public bool AppsUseLightTheme()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true); ;
            var keyValue = registryKey.GetValue("AppsUseLightTheme");
            if ((int)keyValue == 1) return true;
            else return false;
        }
    }
}
