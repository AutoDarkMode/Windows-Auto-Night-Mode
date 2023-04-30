// Copyright (c) 2022 namazso <admin@namazso.eu>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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

using AutoDarkModeLib.IThemeManager2;
using AutoDarkModeSvc.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using static AutoDarkModeLib.IThemeManager2.Flags;

namespace AutoDarkModeSvc.Handlers.IThemeManager2
{

    public static class Tm2Handler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static GlobalState state = GlobalState.Instance();

        [DllImport("ole32.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int CoCreateInstance(
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid rclsid,
            IntPtr pUnkOuter,
            uint dwClsContext,
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppv
        );

        private static Interfaces.IThemeManager2 InitManager()
        {
            var hr = CoCreateInstance(
                       Guid.Parse("9324da94-50ec-4a14-a770-e90ca03e7c8f"),
                       IntPtr.Zero,
                       0x17,
                       typeof(Interfaces.IThemeManager2).GUID,
                       out var obj);
            if (obj == null)
            {
                throw new ExternalException($"cannot create IThemeManager2 instance: {hr:x8}!", hr);
            }
            var manager = (Interfaces.IThemeManager2)obj;
            manager.Init(InitializationFlags.ThemeInitNoFlags);
            return manager;
        }

        /// <summary>
        /// Sets a theme via the provided theme index
        /// </summary>
        /// <param name="theme">the theme wrapper file that should be used to apply the theme, containing the index</param>
        /// <param name="manager">the theme manager com object that MUST be within the same thread as this method</param>
        /// <param name="flags">application flags to ignore certain parts of theme switching</param>
        /// <returns></returns>
        /// <exception cref="ExternalException"></exception>
        private static bool SetThemeViaIdx(Theme2Wrapper theme, Interfaces.IThemeManager2 manager, ThemeApplyFlags flags)
        {

            int res = manager.SetCurrentTheme(IntPtr.Zero, theme.Idx, 1, flags, 0);
            if (res != 0)
            {
                throw new ExternalException($"error setting theme via id, hr: {res}", res);
            }
            return true;
        }
        /// <summary>
        /// This does not work. ignores the silent flag. opens control panel when switching themes.
        /// Use at your own risk...
        /// </summary>
        /// <param name="path"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ExternalException"></exception>
        private static bool SetThemeViaPath(string path, Interfaces.IThemeManager2 manager)
        {
            int res = manager.OpenTheme(IntPtr.Zero, path, ThemePackFlags.Silent);
            if (res != 0)
            {
                throw new ExternalException($"error setting theme via path, hr: {res}", res);
            }
            return true;
        }

        private static bool SetThemeViaAddAndSelect(string path, Interfaces.IThemeManager2 manager, ThemeApplyFlags flags)
        {
            int res = manager.AddAndSelectTheme(IntPtr.Zero, path, flags, ThemePackFlags.Silent);
            if (res != 0)
            {
                throw new ExternalException($"error setting theme via path, hr: {res}", res);
            }
            return true;
        }

        public static (bool, string) GetActiveThemeName()
        {
            bool isCustom = false;
            string displayName = null;
            Thread thread = new(() =>
            {
                Interfaces.IThemeManager2 manager = null;
                Interfaces.ITheme theme = null;
                try
                {
                    manager = InitManager();
                    manager.GetCurrentTheme(out int idxCurrent);
                    manager.GetCustomTheme(out int idxCustom);
                    if (idxCurrent == idxCustom)
                    {
                        manager.UpdateCustomTheme();
                        isCustom = true;
                    }
                    manager.GetTheme(idxCurrent, out theme);
                    displayName = theme.DisplayName;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not get active theme name via IThemeManager2");
                }
                finally
                {
                    if (theme != null) Marshal.ReleaseComObject(theme);
                    if (manager != null) Marshal.ReleaseComObject(manager);
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }
            return (isCustom, displayName);
        }

        /// <summary>
        /// Sets a theme given a path
        /// </summary>
        /// <param name="path">the path of the theme file</param>
        /// <returns>the first tuple entry is true if the theme was found, the second is true if theme switching was successful</returns>
        public static (bool, bool) SetTheme(string displayName, string originalPath, List<ThemeApplyFlags> flagList = null, bool suppressLogging = false)
        {
            bool found = false;
            bool success = false;

            if (displayName == null)
            {
                return (found, success);
            }

            ThemeApplyFlags flags = 0;
            if (flagList != null && flagList.Count > 0)
            {
                flagList.ForEach(f =>
                {
                    // never allow color ignore flag to be set as this breaks win 11 theme switching
                    if (f != ThemeApplyFlags.IgnoreColor) flags |= f;
                });
            }

            Thread thread = new(() =>
            {
                Interfaces.IThemeManager2 manager = null;
                try
                {
                    manager = InitManager();

                    if (state.LearnedThemeNames.ContainsKey(displayName))
                    {
                        Logger.Debug($"using learned theme name: {displayName}={state.LearnedThemeNames[displayName]}");
                        displayName = state.LearnedThemeNames[displayName];
                    }

                    List<Theme2Wrapper> themes = GetThemeList(manager);

                    if (themes.Count > 0)
                    {
                        Theme2Wrapper targetTheme = themes.Where(t => t.ThemeName == displayName).FirstOrDefault();
                        if (targetTheme != null)
                        {
                            found = true;

                            // Using this enables setting themes without explicitly knowing the display name. May be useful for later
                            // success = SetThemeViaAddAndSelect(originalPath, manager, flags);
                            success = SetThemeViaIdx(targetTheme, manager, flags);
                            if (success)
                            {
                                if (!suppressLogging) Logger.Info($"applied theme {targetTheme.ThemeName}, from origin: {originalPath} directly via IThemeManager2");
                            }
                        }
                        else
                        {
                            success = true;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"could not apply theme via IThemeManager2");
                }
                finally
                {
                    if (manager != null) Marshal.ReleaseComObject(manager);
                }
            })
            {
                Name = "COMThemeManagerThread"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            try
            {
                thread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "theme handler thread was interrupted");
            }

            return (found, success);

        }

        private static List<Theme2Wrapper> GetThemeList(Interfaces.IThemeManager2 manager)
        {
            List<Theme2Wrapper> list = new();

            int count = 0;
            try
            {
                int res = manager.GetThemeCount(out count);
                if (res != 0)
                {
                    throw new ExternalException($"StatusCode: {res}", res);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"exception in Source GetThemeList->GetCount: ");
                throw;
            }

            for (int i = 0; i < count; i++)
            {
                Interfaces.ITheme theme = null;
                try
                {
                    manager.GetTheme(i, out theme);

                    list.Add(new()
                    {
                        Idx = i,
                        ThemeName = theme.DisplayName
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"exception in Source GetThemeList->GetCount: ");
                    throw;
                }
                finally
                {
                    if (theme != null) Marshal.ReleaseComObject(theme);
                }
            }

            return list;
        }
    }

    public class Theme2Wrapper
    {
        public string ThemeName { get; set; }
        public int Idx { get; set; }
    }
}

