using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class TouchKeyboardSwitch : BaseComponent<object>
    {
        private Theme currentComponentTheme = Theme.Unknown;
        private readonly string keyboardPersonalizationKey = @"Software\Microsoft\TabletTip\1.7";
        public override bool ThemeHandlerCompatibility { get; } = true;

        public TouchKeyboardSwitch() : base() { }

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (currentComponentTheme != e.Theme)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            DeleteTouchKeyboardRegistryKeys();
            currentComponentTheme = e.Theme;
        }

        protected override void EnableHook()
        {
            currentComponentTheme = Theme.Unknown;
        }

        private void DeleteTouchKeyboardRegistryKeys()
        {
            try
            {
                RegistryKey mainKey = Registry.CurrentUser.OpenSubKey(keyboardPersonalizationKey, true);
                mainKey.DeleteValue("SelectedThemeIndex", false);
                mainKey.DeleteValue("SelectedThemeName", false);
                mainKey.DeleteSubKeyTree("SelectedThemeDark", false);
                mainKey.DeleteSubKeyTree("SelectedThemeLight", false);
                mainKey.Close();
            }
            catch
            {
                Logger.Warn("could not find or delete the touch keyboard registry keys");
            }
        }
    }
}
