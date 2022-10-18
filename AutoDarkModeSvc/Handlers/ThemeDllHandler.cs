using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using NLog;
using System;
using System.Diagnostics;

namespace AutoDarkModeSvc.Handlers
{
    internal class ThemeDllHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Sets a theme given a path via a bridging application
        /// </summary>
        /// <param name="path">the path of the theme file</param>
        /// <returns>the first tuple entry is true if the theme was found, the second is true if theme switching was successful</returns>
        public static (bool, bool) SetThemeViaBridge(string displayName)
        {

            Process bridge = new();
            bridge.StartInfo.FileName = Helper.ExectuionPathThemeBridge;
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
                bool success = Enum.TryParse(response.StatusCode, out BridgeResponseCode statusCode);
                if (success)
                {
                    if (statusCode == BridgeResponseCode.Success)
                    {
                        Logger.Info($"applied theme {displayName} successfully via IThemeManager2");
                        return (true, true);
                    }
                    else if (statusCode == BridgeResponseCode.NotFound)
                    {
                        return (false, true);
                    }
                    else if (statusCode == BridgeResponseCode.InvalidArguments) return (false, false);
                    else if (statusCode == BridgeResponseCode.Fail) return (false, false);
                }
                if (response.Message != null)
                {
                    Logger.Error($"failed to apply theme via ThemeManager2: {response.Message}");
                }
            }
            return (false, false);
        }
    }
}
