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
using System;
using System.Diagnostics;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using NLog;

namespace AutoDarkModeSvc.Handlers;

internal class ThemeDllHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Sets a theme given a path via a bridging application
    /// </summary>
    /// <param name="path">the path of the theme file</param>
    /// <returns>the first tuple entry is true if the theme was found, the second is true if theme switching was successful</returns>
    public static (bool, bool) SetThemeViaBridge(string displayName)
    {

        Process bridge = new();
        bridge.StartInfo.FileName = Helper.ExecutionPathThemeBridge;
        bridge.StartInfo.ArgumentList.Add(displayName);
        bridge.StartInfo.RedirectStandardOutput = true;
        bridge.Start();
        string line = "";
        while (!bridge.StandardOutput.EndOfStream)
        {
            line += bridge.StandardOutput.ReadLine();
        }
        bridge.WaitForExit();
        int exitCode = bridge.ExitCode;
        if (exitCode == 0)
        {
            ApiResponse response = ApiResponse.FromString(line);
            if (Enum.TryParse(response.StatusCode, out BridgeResponseCode statusCode))
            {
                switch (statusCode)
                {
                    case BridgeResponseCode.Success:
                        Logger.Info($"applied theme {displayName} successfully via IThemeManager2");
                        return (true, true);
                    case BridgeResponseCode.NotFound:
                        return (false, true);
                    case BridgeResponseCode.InvalidArguments:
                        return (false, false);
                    case BridgeResponseCode.Fail:
                        return (false, false);
                }
            }
            if (response.Message != null)
            {
                Logger.Error($"failed to apply theme via ThemeManager2: {response.Message}");
            }
        }
        return (false, false);
    }
}
