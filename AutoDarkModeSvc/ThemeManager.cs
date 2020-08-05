using System;
using AutoDarkModeSvc.Handlers;
using System.Threading.Tasks;
using AutoDarkModeSvc.Config;
using System.Threading;
using System.IO;
using Windows.System.Power;
using System.Collections.Generic;

namespace AutoDarkModeSvc
{
    static class ThemeManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void TimedSwitch(AdmConfigBuilder builder)
        {
            GlobalState state = GlobalState.Instance();
            if (state.ForcedTheme == Theme.Dark) 
            {
                SwitchTheme(builder.Config, Theme.Dark);
                return;
            } 
            else if (state.ForcedTheme == Theme.Light) 
            {
                SwitchTheme(builder.Config, Theme.Light);
                return;
            }

            DateTime sunrise = builder.Config.Sunrise;
            DateTime sunset = builder.Config.Sunset;
            if (builder.Config.Location.Enabled)
            {
                LocationHandler.GetSunTimesWithOffset(builder, out sunrise, out sunset);
            }
            //the time bewteen sunrise and sunset, aka "day"
            if (Extensions.NowIsBetweenTimes(sunrise.TimeOfDay, sunset.TimeOfDay))
            {
                // ensure that the theme doesn't switch to light mode if the battery is discharging
                if (builder.Config.Events.DarkThemeOnBattery && PowerManager.BatteryStatus != BatteryStatus.Discharging)
                {
                    SwitchTheme(builder.Config, Theme.Light, true, sunset, sunrise);
                }
                else if (!builder.Config.Events.DarkThemeOnBattery)
                {
                    SwitchTheme(builder.Config, Theme.Light, true, sunset, sunrise);
                }
            }
            else
            {
                SwitchTheme(builder.Config, Theme.Dark, true, sunset, sunrise);
            }
        }

        public static void SwitchTheme(AdmConfig config, Theme newTheme, bool automatic = false, DateTime sunset = new DateTime(), DateTime sunrise = new DateTime())
        {
            if (config.ClassicMode)
            {
                ApplyThemeOptions(config, newTheme, automatic, sunset, sunrise);
            }
            else
            {
                ApplyTheme(config, newTheme, automatic, sunset, sunrise);
            }
        }

        private static void ApplyTheme(AdmConfig config, Theme newTheme, bool automatic, DateTime sunset, DateTime sunrise)
        {
            GlobalState state = GlobalState.Instance();
            if (config.DarkThemePath == null || config.LightThemePath == null)
            {
                Logger.Error("dark or light theme path empty");
                return;
            }
            if (!File.Exists(config.DarkThemePath)) {
                Logger.Error($"invalid dark theme path: {config.DarkThemePath}");
                return;
            }
            if (!File.Exists(config.LightThemePath))
            {
                Logger.Error($"invalid light theme path : {config.LightThemePath}");
                return;
            }
            if (!config.DarkThemePath.EndsWith(".theme") || !config.DarkThemePath.EndsWith(".theme"))
            {
                Logger.Error("both theme paths must have a .theme extension");
                return;
            }

            if (Path.GetFileNameWithoutExtension(config.DarkThemePath) != state.CurrentWindowsThemeName && newTheme == Theme.Dark)
            {
                if (automatic)
                {
                    Logger.Info($"automatic dark theme switch pending, sunset: {sunset.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info("switching to dark theme");
                }
                SetColorFilter(config.ColorFilterEnabled, newTheme);
                SetOfficeTheme(config.Office.Mode, newTheme, state, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);
                ThemeHandler.Apply(config.DarkThemePath);
            }
            else if (Path.GetFileNameWithoutExtension(config.LightThemePath) != state.CurrentWindowsThemeName && newTheme == Theme.Light)
            {
                if (automatic)
                {
                    Logger.Info($"automatic light theme switch pending, sunrise: {sunrise.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info("switching to light theme");
                }
                SetColorFilter(config.ColorFilterEnabled, newTheme);
                SetOfficeTheme(config.Office.Mode, newTheme, state, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);
                ThemeHandler.Apply(config.LightThemePath);            }
        }

        private static void ApplyThemeOptions(AdmConfig config, Theme newTheme, bool automatic, DateTime sunset, DateTime sunrise)
        {
            GlobalState state = GlobalState.Instance();

            if (!ThemeOptionsNeedUpdate(config, newTheme))
            {
                return;
            }
            PowerHandler.DisableEnergySaver(config);
            var oldsys = state.CurrentSystemTheme;
            var oldapp = state.CurrentAppsTheme;
            var oldedg = state.CurrentEdgeTheme;
            var oldwal = state.CurrentWallpaperTheme;
            var oldoff = state.CurrentOfficeTheme;

            SetColorFilter(config.ColorFilterEnabled, newTheme);
            SetAppsTheme(config.AppsTheme, newTheme, state);
            SetEdgeTheme(config.EdgeTheme, newTheme, state);

            SetWallpaper(newTheme, state, config.Wallpaper.DarkThemeWallpapers, config.Wallpaper.LightThemeWallpapers, config.Wallpaper.Enabled);
            SetOfficeTheme(config.Office.Mode, newTheme, state, config.Office.LightTheme, config.Office.DarkTheme, config.Office.Enabled);
            //run async to delay at specific parts due to color prevalence not switching icons correctly
            int taskdelay = config.Tunable.AccentColorSwitchDelay;
            Task.Run(async () =>
            {
                await SetSystemTheme(config.SystemTheme, newTheme, taskdelay, state, config);

                if (automatic)
                {
                    Logger.Info($"theme switch invoked automatically. Sunrise: {sunrise.ToString("HH:mm:ss")}, Sunset: {sunset.ToString("HH:mm:ss")}");
                }
                else
                {
                    Logger.Info($"theme switch invoked manually");
                }
                PowerHandler.RestoreEnergySaver(config);
                Logger.Info($"theme: {newTheme} with modes (s:{config.SystemTheme}, a:{config.AppsTheme}, e:{config.EdgeTheme}, w:{config.Wallpaper.Enabled}, o:{config.Office.Enabled})");
                Logger.Info($"was (s:{oldsys}, a:{oldapp}, e:{oldedg}, w:{oldwal}, o:{oldoff})");
                Logger.Info($"is (s:{state.CurrentSystemTheme}, a:{state.CurrentAppsTheme}, e:{state.CurrentEdgeTheme}, w:{state.CurrentWallpaperTheme}, o:{state.CurrentOfficeTheme})");
            });
        }

        private static void SetAppsTheme(Mode mode, Theme newTheme, GlobalState rtc)
        {
            if (mode == Mode.DarkOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Dark);
                rtc.CurrentAppsTheme = Theme.Dark;
            }
            else if (mode == Mode.LightOnly)
            {
                RegistryHandler.SetAppsTheme((int)Theme.Light);
                rtc.CurrentAppsTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetAppsTheme((int)newTheme);
                rtc.CurrentAppsTheme = newTheme;
            }
        }

        private async static Task SetSystemTheme(Mode mode, Theme newTheme, int taskdelay, GlobalState state, AdmConfig config)
        {
            // Set system theme
            if (mode == Mode.DarkOnly)
            {
                if (state.CurrentSystemTheme != Theme.Dark)
                {
                    RegistryHandler.SetSystemTheme((int)Theme.Dark);
                }
                else
                {
                    taskdelay = 0;
                }
                state.CurrentSystemTheme = Theme.Dark;
                await Task.Delay(taskdelay);
                if (config.AccentColorTaskbarEnabled)
                {
                    RegistryHandler.SetColorPrevalence(1);
                    state.CurrentColorPrevalence = true;
                }
                else if (!config.AccentColorTaskbarEnabled && state.CurrentColorPrevalence == true)
                {
                    RegistryHandler.SetColorPrevalence(0);
                    state.CurrentColorPrevalence = false;
                }
            }
            else if (config.SystemTheme == Mode.LightOnly)
            {
                RegistryHandler.SetColorPrevalence(0);
                state.CurrentColorPrevalence = false;
                await Task.Delay(taskdelay);
                RegistryHandler.SetSystemTheme((int)Theme.Light);
                state.CurrentSystemTheme = Theme.Light;
            }
            else
            {
                if (newTheme == Theme.Light)
                {
                    RegistryHandler.SetColorPrevalence(0);
                    state.CurrentColorPrevalence = false;
                    await Task.Delay(taskdelay);
                    RegistryHandler.SetSystemTheme((int)newTheme);
                } 
                else if (newTheme == Theme.Dark)
                {
                    if (state.CurrentSystemTheme == Theme.Dark)
                    {
                        taskdelay = 0;
                    }
                    RegistryHandler.SetSystemTheme((int)newTheme);
                    if (config.AccentColorTaskbarEnabled)
                    {
                        await Task.Delay(taskdelay);
                        RegistryHandler.SetColorPrevalence(1);
                        state.CurrentColorPrevalence = true;
                    }
                    else
                    {
                        await Task.Delay(taskdelay);
                        RegistryHandler.SetColorPrevalence(0);
                        state.CurrentColorPrevalence = false;
                    }
                }
                state.CurrentSystemTheme = newTheme;
            }
        }

        private static void SetEdgeTheme(Mode mode, Theme newTheme, GlobalState rtc)
        {
            if (mode == Mode.DarkOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Dark);
                rtc.CurrentEdgeTheme = Theme.Dark;
            }
            else if (mode == Mode.LightOnly)
            {
                RegistryHandler.SetEdgeTheme((int)Theme.Light);
                rtc.CurrentEdgeTheme = Theme.Light;
            }
            else
            {
                RegistryHandler.SetEdgeTheme((int)newTheme);
                rtc.CurrentEdgeTheme = newTheme;
            }
        }

        private static void SetOfficeTheme(Mode mode, Theme newTheme, GlobalState rtc, byte lightTheme, byte darkTheme, bool enabled)
        {
            if (enabled)
            {
                if (mode == Mode.DarkOnly)
                {
                    RegistryHandler.OfficeTheme(darkTheme);
                    rtc.CurrentOfficeTheme = Theme.Dark;
                }
                else if (mode == Mode.LightOnly)
                {
                    RegistryHandler.OfficeTheme(lightTheme);
                    rtc.CurrentOfficeTheme = Theme.Light;
                }
                else
                {
                    if (newTheme == Theme.Dark)
                    {
                        RegistryHandler.OfficeTheme(darkTheme);
                    }
                    else
                    {
                        RegistryHandler.OfficeTheme(lightTheme);
                    }
                    rtc.CurrentOfficeTheme = newTheme;
                }
            }
        }

        private static void SetColorFilter(bool enabled, Theme newTheme)
        {
            if (enabled)
            {
                try
                {
                    RegistryHandler.ColorFilterSetup();
                    if (newTheme == Theme.Dark)
                    {
                        RegistryHandler.ColorFilterKeySender(true);
                    }
                    else
                    {
                        RegistryHandler.ColorFilterKeySender(false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "could not enable color filter:");
                }
            }
        }

        private static void SetWallpaper(Theme newTheme, GlobalState rtc, List<string> darkThemeWallpapers,
            List<string> lightThemeWallpapers, bool enabled)
        {
            if (enabled)
            {
                if (newTheme == Theme.Dark)
                {
                    var success = WallpaperHandler.SetBackground(darkThemeWallpapers);
                    if (success)
                    {
                        rtc.CurrentWallpaperTheme = newTheme;
                        rtc.CurrentWallpaperPath = WallpaperHandler.GetBackground();
                    }
                }
                else
                {
                    var success = WallpaperHandler.SetBackground(lightThemeWallpapers);
                    if (success)
                    {
                        rtc.CurrentWallpaperTheme = newTheme;
                        rtc.CurrentWallpaperPath = WallpaperHandler.GetBackground();
                    }
                }
            }
        }

        /// <summary>
        /// checks if any theme needs an update because the runtimeconfig differs from the actual configured state
        /// </summary>
        /// <param name="config">AutoDarkModeConfig instance</param>
        /// <param name="newTheme">new theme that is requested</param>
        /// <returns></returns>
        private static bool ThemeOptionsNeedUpdate(AdmConfig config, Theme newTheme)
        {
            GlobalState state = GlobalState.Instance();
            if (config.Wallpaper.Enabled)
            {
                if (state.CurrentWallpaperTheme == Theme.Undefined || state.CurrentWallpaperTheme != newTheme)
                {
                    return true;
                }
            }

            if (config.SystemTheme == Mode.Switch)
            {
                if (config.AccentColorTaskbarEnabled && state.CurrentColorPrevalence && newTheme == Theme.Light)
                {
                    return true;
                }
                else if ((!config.AccentColorTaskbarEnabled && state.CurrentColorPrevalence) 
                    || (config.AccentColorTaskbarEnabled && !state.CurrentColorPrevalence && newTheme == Theme.Dark))
                {
                    return true;
                }
            }
            else if (config.SystemTheme == Mode.DarkOnly)
            {
                if ((!config.AccentColorTaskbarEnabled && state.CurrentColorPrevalence) || (config.AccentColorTaskbarEnabled && !state.CurrentColorPrevalence))
                {
                    return true;
                }
            }
        
            if (ComponentNeedsUpdate(config.SystemTheme, state.CurrentSystemTheme, newTheme))
            {
                return true;
            }

            if (ComponentNeedsUpdate(config.AppsTheme, state.CurrentAppsTheme, newTheme))
            {
                return true;
            }

            if (ComponentNeedsUpdate(config.EdgeTheme, state.CurrentEdgeTheme, newTheme))
            {
                return true;
            }
            
            if (WallpaperNeedsUpdate(config.Wallpaper.Enabled, state.CurrentWallpaperPath, config.Wallpaper.LightThemeWallpapers, 
                config.Wallpaper.DarkThemeWallpapers, state.CurrentWallpaperTheme, newTheme))
            {
                return true;
            }

            if (config.Office.Enabled && ComponentNeedsUpdate(config.Office.Mode, state.CurrentOfficeTheme, newTheme))
            {
                return true;
            }

            if (config.ColorFilterEnabled && ColorFilterNeedsUpdate(config.ColorFilterEnabled, state.ColorFilterEnabled, newTheme))
            {
                return true;
            }

            return false;
        }

        private static bool ComponentNeedsUpdate(Mode componentMode, Theme currentComponentTheme, Theme newTheme)
        {
            if ((componentMode == Mode.DarkOnly && currentComponentTheme != Theme.Dark)
                || (componentMode == Mode.LightOnly && currentComponentTheme != Theme.Light)
                || (componentMode == Mode.Switch && currentComponentTheme != newTheme)
              )
            {
                return true;
            }
            return false;
        }

        private static bool WallpaperNeedsUpdate(bool enabled, string currentWallpaperPath, List<string> lightThemeWallpapers, 
            List<string> darkThemeWallpapers, Theme currentComponentTheme, Theme newTheme)
        {
            if (enabled)
            {
                if (currentComponentTheme != newTheme)
                {
                    return true;
                } 
                else if (newTheme == Theme.Dark && !darkThemeWallpapers.Contains(currentWallpaperPath))
                {
                    return true;
                } 
                else if (newTheme == Theme.Light && !lightThemeWallpapers.Contains(currentWallpaperPath))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ColorFilterNeedsUpdate(bool colorFilterEnabled, bool currentColorFilterEnabled, Theme newTheme)
        {
            if (!colorFilterEnabled && currentColorFilterEnabled)
            {
                return true;
            }
            if (!currentColorFilterEnabled && newTheme == Theme.Dark) {
                return true;
            } 
            else if (currentColorFilterEnabled && newTheme == Theme.Light)
            {
                return true;
            }
            return false;
        }
    }
}
