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
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class SystemSwitchThemeFile : BaseComponent<SystemSwitchSettings>
    {
        protected Theme currentComponentTheme = Theme.Unknown;
        protected bool themeModeEnabled;
        protected bool currentTaskbarColorActive;
        public SystemSwitchThemeFile() : base() { }

        protected override void EnableHook()
        {
            RefreshRegkeys();
        }

        protected void RefreshRegkeys()
        {
            try
            {
                currentComponentTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;
                currentTaskbarColorActive = RegistryHandler.IsColorPrevalence();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize system apps theme state");
            }
        }
        public override bool ThemeHandlerCompatibility => true;

        protected override bool ComponentNeedsUpdate(SwitchEventArgs e)
        {
            if (Settings.Component.Mode == Mode.AccentOnly)
            {
                NeedsDwmRefresh = DwmRefreshType.Standard;
                TriggersDwmRefresh = DwmRefreshType.None;
                // if theme does not match dark we need to report true, as accent color isn't available in light mode
                // Do not return true on windows theme mode, as this would potentially modify the theme
                if (currentComponentTheme != Theme.Dark && !themeModeEnabled) return true;

                if (e.Theme == Theme.Dark)
                {
                    // allow toggling of the taskbar color in dark mode if it is not active yet, or still active
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && !currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && currentTaskbarColorActive) return true;
                }
                else if (e.Theme == Theme.Light)
                {
                    // allow toggling of the taskbar color in light mode if it is not active yet, or still active (inverse of Theme.Dark if clause)
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && !currentTaskbarColorActive) return true;
                }
                return false;
            }

            if (themeModeEnabled)
            {
                return false;
            }
            else if (Settings.Component.Mode == Mode.DarkOnly)
            {
                // Themes do not match
                if (currentComponentTheme != Theme.Dark)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.Standard;
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.None;
                    return true;
                }
                // task bar accent color should switch, and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.None;
                    return true;
                }
                return false;

            }
            else if (Settings.Component.Mode == Mode.LightOnly)
            {
                if (currentComponentTheme != Theme.Light)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.Standard;
                    return true;
                }
                return false;
            }
            else if (Settings.Component.Mode == Mode.Switch)
            {
                // Themes do not match
                if (currentComponentTheme != e.Theme)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.Standard;
                    return true;
                }
                // Task bar accent color should switch, target is light mode and the taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive && e.Theme == Theme.Light)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.None;
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.None;
                    return true;
                }
                // task bar accent color should switch, target is dark mode and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive && e.Theme == Theme.Dark)
                {
                    NeedsDwmRefresh = DwmRefreshType.Standard;
                    TriggersDwmRefresh = DwmRefreshType.None;
                    return true;
                }
                return false;
            }
            return false;
        }

        protected override void HandleSwitch(SwitchEventArgs e)
        {
            Task.Run(async () => { await SwitchSystemTheme(e.Theme); }).Wait();
        }

        protected async virtual Task SwitchSystemTheme(Theme newTheme)
        {
            bool oldAccent = currentTaskbarColorActive;
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            int taskdelay = Settings.Component.TaskbarSwitchDelay;
            try
            {
                // Set system theme
                if (Settings.Component.Mode == Mode.AccentOnly)
                {
                    await SwitchAccentOnly(newTheme, taskdelay);
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    SwitchLightOnly();
                }
                else if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    SwitchDarkOnly();
                }
                else
                {
                    SwitchAdaptive(newTheme);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set system theme");
            }
            string accentInfo;
            if (Settings.Component.Mode == Mode.AccentOnly)
            {
                accentInfo = $"on {Enum.GetName(typeof(Theme), Settings.Component.TaskbarColorWhenNonAdaptive).ToLower()}";
            }
            else
            {
                accentInfo = Settings.Component.TaskbarColorOnAdaptive ? "yes" : "no";
            }
            Logger.Info($"update info - previous: {oldTheme}/{(oldAccent ? "Accent" : "NoAccent")}, " +
                $"pending: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{(currentTaskbarColorActive ? "Accent" : "NoAccent")}, " +
                $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
                $"accent: {accentInfo}");
        }

        protected async Task SwitchAccentOnly(Theme newTheme, int taskdelay)
        {
            if (currentComponentTheme != Theme.Dark && !themeModeEnabled)
            {
                RegistryHandler.SetSystemTheme((int)Theme.Dark);
            }
            else
            {
                taskdelay = 0;
            }
            await Task.Delay(taskdelay);

            if (newTheme == Theme.Dark)
            {
                if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            else if (newTheme == Theme.Light)
            {
                if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            currentComponentTheme = Theme.Dark;
        }

        protected void SwitchLightOnly()
        {
            if (Settings.Component.TaskbarColorOnAdaptive)
            {
                RegistryHandler.SetColorPrevalence(0);
            }
            currentTaskbarColorActive = false;
            ThemeFile themeFile = GlobalState.ManagedThemeFile;
            themeFile.VisualStyles.SystemMode = (nameof(Theme.Light), themeFile.VisualStyles.SystemMode.Item2);
            currentComponentTheme = Theme.Light;
        }

        protected void SwitchDarkOnly()
        {
            if (currentComponentTheme != Theme.Dark)
            {
                ThemeFile themeFile = GlobalState.ManagedThemeFile;
                themeFile.VisualStyles.SystemMode = (nameof(Theme.Dark), themeFile.VisualStyles.SystemMode.Item2);
            }
            currentComponentTheme = Theme.Dark;
            if (Settings.Component.TaskbarColorOnAdaptive)
            {
                RegistryHandler.SetColorPrevalence(1);
                currentTaskbarColorActive = true;
            }
            else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
            {
                RegistryHandler.SetColorPrevalence(0);
                currentTaskbarColorActive = false;
            }
        }

        protected void SwitchAdaptive(Theme newTheme)
        {
            ThemeFile themeFile = GlobalState.ManagedThemeFile;
            if (newTheme == Theme.Light)
            {
                RegistryHandler.SetColorPrevalence(0);
                currentTaskbarColorActive = false;
                themeFile.VisualStyles.SystemMode = (nameof(Theme.Light), themeFile.VisualStyles.SystemMode.Item2);
            }
            else if (newTheme == Theme.Dark)
            {
                if (currentComponentTheme != Theme.Dark)
                {
                    themeFile.VisualStyles.SystemMode = (nameof(Theme.Dark), themeFile.VisualStyles.SystemMode.Item2);
                }
                currentComponentTheme = Theme.Dark;
                if (Settings.Component.TaskbarColorOnAdaptive)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    currentTaskbarColorActive = true;
                }
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                }
            }
            currentComponentTheme = newTheme;
        }

        protected override void UpdateSettingsState()
        {
            AdmConfigBuilder builder = AdmConfigBuilder.Instance();
            themeModeEnabled = builder.Config.WindowsThemeMode.Enabled;
            RefreshRegkeys();
        }
    }
}
