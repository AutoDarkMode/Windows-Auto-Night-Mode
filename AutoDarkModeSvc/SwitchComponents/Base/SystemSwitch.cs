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
        private Theme currentComponentTheme = Theme.Unknown;
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

        public override bool ComponentNeedsUpdate(Theme newTheme)
        {
            if (Settings.Component.Mode == Mode.DarkOnly)
            {
                // Themes do not match
                if (currentComponentTheme != Theme.Dark)
                {
                    return true;
                }
                else if (newTheme == Theme.Dark)
                {
                    // allow toggling of the taskbar color in dark mode if it is not active yet, or still active
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && !currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && currentTaskbarColorActive) return true;
                }
                else if (newTheme == Theme.Light)
                {
                    // allow toggling of the taskbar color in light mode if it is not active yet, or still active (inverse of Theme.Dark if clause)
                    if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Dark && currentTaskbarColorActive) return true;
                    else if (Settings.Component.TaskbarColorWhenNonAdaptive == Theme.Light && !currentTaskbarColorActive) return true;
                }

                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive)
                {
                    return true;
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
                else if (Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive && newTheme == Theme.Light)
                {
                    return true;
                }
                // Task bar accent color is disabled, but still active
                else if (!Settings.Component.TaskbarColorOnAdaptive && currentTaskbarColorActive)
                {
                    return true;
                }
                // task bar accent color should switch, target is dark mode and taskbar color hasn't switched yet
                else if (Settings.Component.TaskbarColorOnAdaptive && !currentTaskbarColorActive && newTheme == Theme.Dark)
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
            bool oldAccent = currentTaskbarColorActive;
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
                }
                else if (Settings.Component.Mode == Mode.LightOnly)
                {
                    if (Settings.Component.TaskbarColorOnAdaptive)
                    {
                        RegistryHandler.SetColorPrevalence(0);
                        await Task.Delay(taskdelay);
                    }

                    currentTaskbarColorActive = false;
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
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not set system theme");
            }
            string accentInfo;
            if (Settings.Component.Mode == Mode.DarkOnly)
            {
                accentInfo = $"on {Enum.GetName(typeof(Theme), Settings.Component.TaskbarColorWhenNonAdaptive).ToLower()}";
            }
            else
            {
                accentInfo = Settings.Component.TaskbarColorOnAdaptive ? "yes" : "no";
            }
            Logger.Info($"update info - previous: {oldTheme}/{(oldAccent ? "accent" : "no accent")}, " +
                $"now: {Enum.GetName(typeof(Theme), currentComponentTheme)}/{(currentTaskbarColorActive ? "accent" : "no accent")}, " +
                $"mode: {Enum.GetName(typeof(Mode), Settings.Component.Mode)}, " +
                $"accent: {accentInfo}");
        }
    }
}
