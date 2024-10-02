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
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AutoDarkModeSvc.Core
{
    class ComponentManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ComponentManager instance;
        public static ComponentManager Instance()
        {
            instance ??= new ComponentManager();
            return instance;
        }

        private readonly List<ISwitchComponent> Components;
        private AdmConfigBuilder Builder { get; }
        private Theme lastSorting = Theme.Unknown;

        // Components
        private readonly ISwitchComponent AppsSwitch;
        private readonly ISwitchComponent ColorFilterSwitch = new ColorFilterSwitch();
        private readonly ISwitchComponent OfficeSwitch = new OfficeSwitch();
        private readonly ISwitchComponent AccentColorSwitch = new AccentColorSwitch();
        private readonly ISwitchComponent SystemSwitch;
        private readonly ISwitchComponent WallpaperSwitch = new WallpaperSwitch();
        private readonly ISwitchComponent ScriptSwitch = new ScriptSwitch();
        private readonly ISwitchComponent ColorizationSwitch;
        private readonly ISwitchComponent CursorSwitch;
        private readonly ISwitchComponent TouchKeyboardSwitch = new TouchKeyboardSwitch();

        /// <summary>
        /// Instructs all components to refresh their settings objects by injecting a new settings object
        /// from the currently available configuration instance
        /// </summary>
        public void UpdateSettings()
        {
            AppsSwitch?.RunUpdateSettingsState(Builder.Config.AppsSwitch);
            ColorFilterSwitch?.RunUpdateSettingsState(Builder.Config.ColorFilterSwitch);
            OfficeSwitch?.RunUpdateSettingsState(Builder.Config.OfficeSwitch);
            SystemSwitch?.RunUpdateSettingsState(Builder.Config.SystemSwitch);
            AccentColorSwitch?.RunUpdateSettingsState(Builder.Config.SystemSwitch);
            WallpaperSwitch?.RunUpdateSettingsState(Builder.Config.WallpaperSwitch);
            ColorizationSwitch?.RunUpdateSettingsState(Builder.Config.ColorizationSwitch);
            CursorSwitch?.RunUpdateSettingsState(Builder.Config.CursorSwitch);
            TouchKeyboardSwitch?.RunUpdateSettingsState(Builder.Config.TouchKeyboardSwitch);
        }

        public void UpdateScriptSettings()
        {
            ScriptSwitch.RunUpdateSettingsState(Builder.ScriptConfig);
        }

        /// <summary>
        /// Registers all components and handles version specific component management
        /// </summary>
        ComponentManager()
        {
            bool hasUbr = int.TryParse(RegistryHandler.GetUbr(), out int ubr);
            Logger.Info($"current windows build: {Environment.OSVersion.Version.Build}.{(hasUbr ? ubr : 0)}");

            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                Logger.Info($"using apps and system components for newer builds {(int)WindowsBuilds.MinBuildForNewFeatures} and up");
                SystemSwitch = new SystemSwitchThemeFile();
                AppsSwitch = new AppsSwitchThemeFile();
            }
            else if (Environment.OSVersion.Version.Build < (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                Logger.Info($"using app and system components for legacy builds");
                SystemSwitch = new SystemSwitch();
                AppsSwitch = new AppsSwitch();
            }

            if (hasUbr &&
               ((Environment.OSVersion.Version.Build == (int)WindowsBuilds.Win11_22H2 && ubr >= (int)WindowsBuildsUbr.Win11_22H2_Spotlight) ||
               Environment.OSVersion.Version.Build > (int)WindowsBuilds.Win11_22H2))
            {
                Logger.Info($"using wallpaper component for windows builds {(int)WindowsBuilds.Win11_22H2}.{(int)WindowsBuildsUbr.Win11_22H2_Spotlight} and up");
                WallpaperSwitch = new WallpaperSwitchThemeFile();
            }
            else
            {
                WallpaperSwitch = new WallpaperSwitch();
                Logger.Info($"using default wallpaper component");
            }

            Builder = AdmConfigBuilder.Instance();
            Components = new List<ISwitchComponent>
            {
                AppsSwitch,
                ColorFilterSwitch,
                OfficeSwitch,
                SystemSwitch,
                AccentColorSwitch,
                WallpaperSwitch,
                ScriptSwitch,
                TouchKeyboardSwitch
            };
            if (Environment.OSVersion.Version.Build >= (int)WindowsBuilds.MinBuildForNewFeatures)
            {
                Logger.Info($"using colorization and cursor switcher for newer builds {(int)WindowsBuilds.MinBuildForNewFeatures} and up");
                ColorizationSwitch = new ColorizationSwitch();
                Components.Add(ColorizationSwitch);
                CursorSwitch = new CursorSwitch();
                Components.Add(CursorSwitch);
            }
            UpdateSettings();
            UpdateScriptSettings();
        }

        /// <summary>
        /// Calls the disable hooks for all themes incompatible with the theme mode
        /// </summary>
        public void InvokeDisableIncompatible()
        {
            Components.ForEach(c =>
            {
                if (!c.ThemeHandlerCompatibility)
                {
                    c.RunDisableHook();
                }
            });
        }

        public void RunAllEnableHooks()
        {
            Components.ForEach(c => c.RunEnableHook());
        }

        /// <summary>
        /// Sets the one time force flag for all modules
        /// </summary>
        public void ForceAll()
        {
            Components.ForEach(c => c.ForceSwitch = true);
        }

        /// <summary>
        /// Checks whether components need an update, which is true: <br/>
        /// if their <see cref="ISwitchComponent.RunComponentNeedsUpdate(Theme)"/> method returns true <br/>
        /// or the module has the force switch flag enabled <br/>
        /// or the module was disabled and is still initialized
        /// <param name="newTheme">The theme that should be checked against</param>
        /// <returns>a list of components that require an update and a boolean that informs whether a dwm refresh needs to be initiated manually</returns>
        /// </summary>
        public (List<ISwitchComponent>, DwmRefreshType needsDwmRefresh, DwmRefreshType dwmRefreshType) GetComponentsToUpdate(SwitchEventArgs e)
        {
            List<ISwitchComponent> shouldUpdate = new();
            DwmRefreshType needsDwmRefresh = DwmRefreshType.None;
            DwmRefreshType dwmRefreshType = DwmRefreshType.None;
            foreach (ISwitchComponent c in Components)
            {
                // require update if theme mode is enabled, the module is enabled and compatible with theme mode
                if (c.Enabled && c.ThemeHandlerCompatibility && Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (!c.Initialized) c.RunEnableHook();

                    if (c.RunComponentNeedsUpdate(e))
                    {
                        AddComponentAndGetDwmInfo(c, shouldUpdate, ref needsDwmRefresh, ref dwmRefreshType);
                    }
                }
                // require update if module is enabled and theme mode is disabled (previously known as classic mode)
                else if (c.Enabled && !Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (!c.Initialized) c.RunEnableHook();

                    if (c.RunComponentNeedsUpdate(e))
                    {
                        AddComponentAndGetDwmInfo(c, shouldUpdate, ref needsDwmRefresh, ref dwmRefreshType);
                    }
                }
                // require update if the component is no longer enabled but still initialized. this will trigger the deinit hook
                else if (!c.Enabled && c.Initialized)
                {
                    // we don't query for dwm info because a module that is about to be disabled is no longer triggered
                    shouldUpdate.Add(c);
                }
                // if the force flag is set to true, we also need to update
                else if (c.ForceSwitch)
                {
                    AddComponentAndGetDwmInfo(c, shouldUpdate, ref needsDwmRefresh, ref dwmRefreshType);
                }
            }

            if (shouldUpdate.Count > 0) Logger.Info($"components queued for update: [{string.Join(", ", shouldUpdate.Select(c => c.GetType().Name.ToString()).ToArray())}]");
            return (shouldUpdate, needsDwmRefresh, dwmRefreshType);
        }

        private static void AddComponentAndGetDwmInfo(ISwitchComponent c, List<ISwitchComponent> shouldUpdate, ref DwmRefreshType needsDwmRefresh, ref DwmRefreshType dwmRefreshType)
        {
            if ((int)c.NeedsDwmRefresh > (int)needsDwmRefresh)
            {
                needsDwmRefresh = c.NeedsDwmRefresh;
            }

            if ((int)c.TriggersDwmRefresh > (int)dwmRefreshType)
            {
                dwmRefreshType = c.TriggersDwmRefresh;
            }
            shouldUpdate.Add(c);
        }

        /// <summary>
        /// Runs all post-sync components in a synchronized context and sorts them prior to execution based on their priorities
        /// </summary>
        /// <param name="components">The components to be run</param>
        /// <param name="newTheme">the requested theme to switch to</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunPostSync(List<ISwitchComponent> components, SwitchEventArgs e)
        {
            if (e.Theme == Theme.Dark && lastSorting != Theme.Dark)
            {
                components.Sort((x, y) => x.PriorityToDark.CompareTo(y.PriorityToDark));
                lastSorting = Theme.Dark;
            }
            else if (e.Theme == Theme.Light && lastSorting != Theme.Light)
            {
                components.Sort((x, y) => x.PriorityToLight.CompareTo(y.PriorityToLight));
                lastSorting = Theme.Light;
            }
            components.ForEach(c =>
            {
                if (c.HookPosition == HookPosition.PostSync)
                {
                    if (Builder.Config.WindowsThemeMode.Enabled && c.ThemeHandlerCompatibility) c.Switch(e);
                    else if (!Builder.Config.WindowsThemeMode.Enabled) c.Switch(e);
                }
            });
        }

        /// <summary>
        /// Runs all pre-sync components in a synchronized context and sorts them prior to execution based on their priorities
        /// </summary>
        /// <param name="components">The components to be run</param>
        /// <param name="newTheme">the requested theme to switch to</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunPreSync(List<ISwitchComponent> components, SwitchEventArgs e)
        {
            if (e.Theme == Theme.Dark && lastSorting != Theme.Dark)
            {
                components.Sort((x, y) => x.PriorityToDark.CompareTo(y.PriorityToDark));
                lastSorting = Theme.Dark;
            }
            else if (e.Theme == Theme.Light && lastSorting != Theme.Light)
            {
                components.Sort((x, y) => x.PriorityToLight.CompareTo(y.PriorityToLight));
                lastSorting = Theme.Light;
            }
            components.ForEach(c =>
            {
                if (c.HookPosition == HookPosition.PreSync)
                {
                    if (Builder.Config.WindowsThemeMode.Enabled && c.ThemeHandlerCompatibility) c.Switch(e);
                    else if (!Builder.Config.WindowsThemeMode.Enabled) c.Switch(e);
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RunCallbacks(List<ISwitchComponent> components, Theme newTheme, SwitchEventArgs e)
        {
            if (newTheme == Theme.Dark && lastSorting != Theme.Dark)
            {
                components.Sort((x, y) => x.PriorityToDark.CompareTo(y.PriorityToDark));
                lastSorting = Theme.Dark;
            }
            else if (newTheme == Theme.Light && lastSorting != Theme.Light)
            {
                components.Sort((x, y) => x.PriorityToLight.CompareTo(y.PriorityToLight));
                lastSorting = Theme.Light;
            }
            components.ForEach(c =>
            {
                c.RunCallback(e);
            });
        }
    }
}
