using AutoDarkModeConfig;
using AutoDarkModeConfig.ComponentSettings.Base;
using AutoDarkModeSvc.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.SwitchComponents.Base
{
    class SystemSwitch : BaseComponent<SystemSwitchSettings>
    {
        private Theme currentComponentTheme = Theme.Undefined;
        private bool currentTaskbarColorActive;
        public SystemSwitch() : base()
        {
            try
            {
                currentComponentTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;
                currentTaskbarColorActive = RegistryHandler.IsColorPrevalence();
            } catch (Exception ex)
            {
                Logger.Error(ex, "couldn't initialize system apps theme state");
            }
        }

        public override bool ThemeHandlerCompatibility { get; } = false;

        protected override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (Settings.Component.Mode == Mode.DarkOnly)
            {
                // Themes do not match
                if (currentComponentTheme != Theme.Dark)
                {
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnDark && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnDark && !currentTaskbarColorActive)
                {

                }
            }
            else if (Settings.Component.Mode == Mode.LightOnly && currentComponentTheme != Theme.Light)
            {
                return true;
            }
            else if (Settings.Component.Mode == Mode.Switch)
            {
                // Themes do not match
                if (currentComponentTheme != newTheme)
                {
                    return true;
                }
                // Task bar accent color should switch, target is light mode and the taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnDark && currentTaskbarColorActive && newTheme == Theme.Light)
                {
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnDark && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, target is dark mode and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnDark && !currentTaskbarColorActive && newTheme == Theme.Dark)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void HandleSwitch(Theme newTheme)
        {
            Task.Run(async() => { await SwitchSystemTheme(newTheme);}).Wait();
        }

        private async Task SwitchSystemTheme(Theme newTheme)
        {
            string oldTheme = Enum.GetName(typeof(Theme), currentComponentTheme);
            int taskdelay = Settings.Component.TaskbarSwitchDelay;
            try
            {
                // Set system theme
                if (Settings.Component.Mode == Mode.DarkOnly)
                {
                    if (currentComponentTheme != Theme.Dark)
                    {
                        RegistryHandler.SetSystemTheme((int)Theme.Dark);
                    }
                    else
                    {
                        taskdelay = 0;
                    }
                    currentComponentTheme = Theme.Dark;
                    await Task.Delay(taskdelay);
                    if (Settings.Component.TaskbarColorOnDark)
                    {
                        RegistryHandler.SetColorPrevalence(1);
                        currentTaskbarColorActive = true;
                    }
                    else if (!Settings.Component.TaskbarColorOnDark && currentTaskbarColorActive)
                    {
                        RegistryHandler.SetColorPrevalence(0);
                        currentTaskbarColorActive = false;
                    }
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    RegistryHandler.SetColorPrevalence(0);
                    currentTaskbarColorActive = false;
                    await Task.Delay(taskdelay);
                    RegistryHandler.SetSystemTheme((int)Theme.Light);
                    currentComponentTheme = Theme.Light;
                }
                else
                {
                    if (newTheme == Theme.Light)
                    {
                        RegistryHandler.SetColorPrevalence(0);
                        currentTaskbarColorActive = false;
                        await Task.Delay(taskdelay);
                        RegistryHandler.SetSystemTheme((int)newTheme);
                    }
                    else if (newTheme == Theme.Dark)
                    {
                        if (currentComponentTheme == Theme.Dark)
                        {
                            taskdelay = 0;
                        }
                        RegistryHandler.SetSystemTheme((int)newTheme);
                        if (Settings.Component.TaskbarColorOnDark)
                        {
                            await Task.Delay(taskdelay);
                            RegistryHandler.SetColorPrevalence(1);
                            currentTaskbarColorActive = true;
                        }
                        else
                        {
                            await Task.Delay(taskdelay);
                            RegistryHandler.SetColorPrevalence(0);
                            currentTaskbarColorActive = false;
                        }
                    }
                    currentComponentTheme = newTheme;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set system theme");
            }
            Logger.Info($"update info - previous: {oldTheme} current: {Enum.GetName(typeof(Theme), currentComponentTheme)}, mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}" +
                $", accent: {(Settings.Component.TaskbarColorOnDark ? "yes" : "no")}");
        }
    }
}
