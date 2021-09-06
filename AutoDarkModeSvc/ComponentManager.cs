using AutoDarkModeConfig;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.SwitchComponents.Base;
using System;
using System.Collections.Generic;
using System.Linq;
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
                if (c.ThemeHandlerCompatibility)
                {
                    c.Switch(newTheme);
                }
            });
        }
    }
}
