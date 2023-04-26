using AutoDarkModeLib;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    internal class ColorizationSwitch : BaseComponent<ColorizationSwitchSettings>
    {
        public override bool ThemeHandlerCompatibility => false;
        public override bool TriggersDwmRefresh => true;
        private bool invalidHexFound = false;
        protected override bool ComponentNeedsUpdate(Theme newTheme)
        {
            bool autoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();
            if (newTheme == Theme.Dark)
            {
                if (autoColorizationState != Settings.Component.DarkAutoColorization) return true;
                else if (invalidHexFound && (Settings.Component.DarkAutoColorization == false)) return false;
                else if (!Settings.Component.DarkAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.DarkHex) return true;
            }
            else if (newTheme == Theme.Light)
            {
                if (autoColorizationState != Settings.Component.LightAutoColorization) return true;
                else if (invalidHexFound && (Settings.Component.LightAutoColorization == false)) return false;
                else if (!Settings.Component.LightAutoColorization && GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#") != Settings.Component.LightHex) return true;
            }
            return false;
        }

        protected override void HandleSwitch(Theme newTheme, SwitchEventArgs e)
        {
            var sortOrderAutoCol = GlobalState.ManagedThemeFile.VisualStyles.AutoColorization.Item2;
            var sortOrderColCol = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item2;
            bool prevAutoColorizationState = GlobalState.ManagedThemeFile.GetAutoColorizationState();

            bool newAutoColorizationState = false;
            if (newTheme == Theme.Dark)
            {
                if (Settings.Component.DarkAutoColorization) newAutoColorizationState = true;
                else newAutoColorizationState = false;
            }
            else if (newTheme == Theme.Light)
            {
                if (Settings.Component.LightAutoColorization) newAutoColorizationState = true;
                else newAutoColorizationState = false;
            }            
            if (newAutoColorizationState) GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("1", sortOrderAutoCol);
            else GlobalState.ManagedThemeFile.VisualStyles.AutoColorization = ("0", sortOrderAutoCol);

            if (prevAutoColorizationState != newAutoColorizationState)
            {
                Logger.Info($"update info - auto colorization - previous {(prevAutoColorizationState ? "enabled" : "disabled")}, " +
                    $"pending: {(newAutoColorizationState ? "enabled" : "disabled")} ({Enum.GetName(typeof(Theme), newTheme).ToString().ToLower()})");
            }

            // if auto colorization is enabled the hex value doesn't matter, so the rest can be skipped.
            // If auto col is or was disabled, then we need to process the hex values
            if (newAutoColorizationState == true)
            {
                return;
            }

            var oldColor = GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor.Item1.Replace("0X", "#");

            string newHex;
            Regex hexValidator = new(Helper.Hegex);

            if (newTheme == Theme.Dark) newHex = Settings.Component.DarkHex;
            else newHex = Settings.Component.LightHex;


            if (!hexValidator.IsMatch(newHex))
            {
                Logger.Warn($"an invalid hex color ({newHex}) found, updating colorization (accent color) disabled until the hex color has been fixed");
                invalidHexFound = true;
                return;
            }
            Logger.Info($"update info - color - previous: {oldColor}, pending: {newHex} ({Enum.GetName(typeof(Theme), newTheme).ToString().ToLower()})");
            newHex = newHex.Replace("#", "0X");
            GlobalState.ManagedThemeFile.VisualStyles.ColorizationColor = (newHex, sortOrderColCol);
        }

        protected override void UpdateSettingsState()
        {
            Regex hexValidator = new(Helper.Hegex);
            if (invalidHexFound && hexValidator.IsMatch(Settings.Component.LightHex) && hexValidator.IsMatch(Settings.Component.DarkHex))
            {
                Logger.Info("invalid hex color has been corrected, component will now function again");
                invalidHexFound = false;
            }
        }

        protected override void Callback()
        {
            
        }
    }
}
