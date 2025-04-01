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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoDarkModeSvc.Handlers;

public static class ScriptHandler
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static void Launch(string name, string path, List<string> args, int? timeoutMillis, string cwd = null)
    {
        try
        {
            int timeoutValue = timeoutMillis ?? 0;
            if (timeoutValue == 0) timeoutValue = 10000;
            if (args == null) args = new();
            string argsString = "";
            argsString = string.Join(" ", args.Select(a => $"\"{a}\""));
            Logger.Info($"running {name}: \"{path}\" {argsString}");
            List<string> stdOut = new();
            List<string> stdErr = new();
            using Process p = new();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = path;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            args.ForEach(a => p.StartInfo.ArgumentList.Add(a));
            if (cwd != null && cwd.Length > 0) p.StartInfo.WorkingDirectory = cwd;
            p.ErrorDataReceived += (sender, line) =>
            {
                if (line.Data != null) stdErr.Add(line.Data);
            };
            p.OutputDataReceived += (sender, line) =>
            {
                if (line.Data != null) stdOut.Add(line.Data);
            };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            bool timeout = !p.WaitForExit(timeoutValue);
            if (!timeout)
            {
                p.WaitForExit();
            }
            if (stdErr.Count != 0)
            {
                Logger.Warn($"{name}'s output does not indicate success: {string.Join("\n", stdErr)}");
            }
            if (stdOut.Count > 0)
            {
                Logger.Info($"{name}'s output: {string.Join("\n", stdOut)}");
            }
            if (timeout)
            {
                p.Kill();
                Logger.Warn($"{name}: {path} {args} took too long (>{timeout}ms) to complete and had to be stopped");
            }
            if (p.ExitCode != 0)
            {
                Logger.Warn($"{name}'s exit code does not indicate success. exit code: {p.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"error while running {name}:");
        }
    }
}
