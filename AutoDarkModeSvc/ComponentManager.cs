using AutoDarkModeConfig;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoDarkModeSvc
{
    class ComponentManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ComponentManager instance;
        public static ComponentManager Instance()
        {
            if (instance == null)
            {
                instance = new ComponentManager();
            }
            return instance;
        }

        private readonly List<ISwitchComponent> Components;
        private AdmConfigBuilder Builder { get; }
        private Theme lastSorting = Theme.Undefined;

        // Components
        private ISwitchComponent AppsSwitch = new AppsSwitch();
        private ISwitchComponent ColorFilterSwitch = new ColorFilterSwitch();
        private ISwitchComponent OfficeSwitch = new OfficeSwitch();
        private ISwitchComponent SystemSwitch = new SystemSwitch();
        //private ISwitchComponent TaskbarAccentColorSwitch;
        private ISwitchComponent WallpaperSwitch = new WallpaperSwitch();
        public void UpdateSettings()
        {
            AppsSwitch.UpdateSettingsState(Builder.Config.AppsSwitch);
            ColorFilterSwitch.UpdateSettingsState(Builder.Config.ColorFilterSwitch);
            OfficeSwitch.UpdateSettingsState(Builder.Config.OfficeSwitch);
            SystemSwitch.UpdateSettingsState(Builder.Config.SystemSwitch);
            WallpaperSwitch.UpdateSettingsState(Builder.Config.WallpaperSwitch);
            //TaskbarAccentcolorSwitch.UpdateSettingsState(Builder.Config.TaskbarAccentColorSwitch);
        }
        ComponentManager()
        {
            Builder = AdmConfigBuilder.Instance();
            Components = new List<ISwitchComponent>
            {
                AppsSwitch,
                ColorFilterSwitch,
                OfficeSwitch,
                SystemSwitch,
                //TaskbarAccentColorSwitch
                WallpaperSwitch
            };
            UpdateSettings();
        }

        public void InvokeDisableHooks()
        {
            Components.ForEach(c => c.DisableHook());
        }

        /// <summary>
        /// Sets the one time force flag for all modules
        /// </summary>
        public void ForceAll()
        {
            Components.ForEach(c =>  c.ForceSwitch = true);
        }

        public bool Check(Theme newTheme)
        {
            bool shouldUpdate = false;
            foreach (ISwitchComponent c in Components)
            {
                // require update if theme mode is enabled, the module is enabled and compatible with theme mode
                if (c.Enabled && c.ThemeHandlerCompatibility && Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (c.ComponentNeedsUpdate(newTheme))
                    {
                        shouldUpdate = true;
                        break;
                    }
                }
                // require update if module is enabled and theme mode is disabled (previously known as classic mode)
                else if (c.Enabled && !Builder.Config.WindowsThemeMode.Enabled)
                {
                    if (c.ComponentNeedsUpdate(newTheme))
                    {
                        shouldUpdate = true;
                        break;
                    }
                }
                // require update if the component is no longer enabled but still initialized. this will trigger the deinit hook
                else if (!c.Enabled && c.Initialized)
                {
                    shouldUpdate = true;
                    break;
                }
                // if the force flag is set to true, we also need to update
                else if (c.ForceSwitch)
                {
                    shouldUpdate = true;
                    break;
                }
            }
            return shouldUpdate;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Run(Theme newTheme)
        {
            if (newTheme == Theme.Dark && lastSorting != Theme.Dark)
            {
                Components.Sort((x, y) => x.PriorityToDark.CompareTo(y.PriorityToDark));
            }
            else if (newTheme == Theme.Light && lastSorting != Theme.Light)
            {
                Components.Sort((x, y) => x.PriorityToLight.CompareTo(y.PriorityToLight));
            }
            Components.ForEach(c =>
            {
                if (Builder.Config.WindowsThemeMode.Enabled && c.ThemeHandlerCompatibility)
                {
                    c.Switch(newTheme);
                }
                else if (!Builder.Config.WindowsThemeMode.Enabled)
                {
                    c.Switch(newTheme);
                }
            });
        }
    }
}
