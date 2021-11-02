using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers
{
    public static class ScriptHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void Launch(string name, string path, List<string> args, string cwd = null)
        {
            try
            {
                if (args == null) args = new();
                string argsString = "";
                argsString = string.Join(" ", args.Select(a => $"\"{a}\""));
                Logger.Info($"running {name}: \"{path}\" {argsString}");
                List<string> stdOut = new();
                List<string> stdErr = new();
                using Process p = new();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = path;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                args.ForEach(a => p.StartInfo.ArgumentList.Add(a));
                if (cwd != null) p.StartInfo.WorkingDirectory = cwd;
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
                bool timeout = !p.WaitForExit(10000);
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
                    Logger.Warn($"{name}: {path} {args} took too long to complete and had to be stopped");
                }
                if (p.ExitCode != 0)
                {
                    Logger.Warn($"{name}'s exit code does not indicate success. exit code: { p.ExitCode }");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"error while running {name}:");
            }
        }
    }
}
