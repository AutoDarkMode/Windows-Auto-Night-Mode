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
        private ISwitchComponent AppsSwitch;
        private ISwitchComponent ColorFilterSwitch;
        //private ISwitchComponent OfficeSwitch;
        private ISwitchComponent SystemSwitch;
        //private ISwitchComponent TaskbarAccentColorSwitch;
        //private ISwitchComponent WallpaperSwitch;
        ComponentManager()
        {
            InitializeComponents();
            Builder = AdmConfigBuilder.Instance();
            Components = new List<ISwitchComponent>
            {
                AppsSwitch,
                ColorFilterSwitch,
                //OfficeSwitch
                SystemSwitch
                //TaskbarAccentColorSwitch
                //WallpaperSwitch
            };
            UpdateSettings();
        }
        public void Run(Theme newTheme)
        {
            if (newTheme == Theme.Dark && lastSorting != Theme.Dark)
            {
                Components.Sort((x, y) => x.PriorityToDark().CompareTo(y.PriorityToDark()));
            }
            else if (newTheme == Theme.Light && lastSorting != Theme.Light)
            {
                Components.Sort((x, y) => x.PriorityToLight().CompareTo(y.PriorityToLight()));
            }
            Components.ForEach(c =>
            {
                if (!Builder.Config.WindowsThemeMode.Enabled || (Builder.Config.WindowsThemeMode.Enabled && c.ThemeHandlerCompatibility))
                {
                    c.Switch(newTheme);
                }
            });
        }
        public void UpdateSettings()
        {
            AppsSwitch.UpdateSettingsState(Builder.Config.AppsSwitch);
            ColorFilterSwitch.UpdateSettingsState(Builder.Config.ColorFilterSwitch);
            //OfficeSwitch.UpdateSettingsState(Builder.Config.OfficeSwitch);
            SystemSwitch.UpdateSettingsState(Builder.Config.SystemSwitch);
            //WallpaperSwitch.UpdateSettingsState(Builder.Config.WallpaperSwitch);
            //TaskbarAccentcolorSwitch.UpdateSettingsState(Builder.Config.TaskbarAccentColorSwitch);
        }

        private void InitializeComponents()
        {
            AppsSwitch = new AppsSwitch();
            ColorFilterSwitch = new ColorFilterSwitch();
            //OfficeSwitch = new OfficeSwitch();
            SystemSwitch = new SystemSwitch();
            //TaskbarAccentColorSwitch = new TaskbarAccentColorSwitch();
            //WallpaperSwitch = new WallpaperSwitch();
        }
    }
}
