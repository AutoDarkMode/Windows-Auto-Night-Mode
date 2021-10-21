using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Handlers
{
    public static class ScriptHandler
    {
        public static void Launch(string fileName, List<string> args)
        {
            using Process startScript = new();
            startScript.StartInfo.UseShellExecute = false;
            startScript.StartInfo.FileName = fileName;
            startScript.StartInfo.UseShellExecute = false;
            startScript.StartInfo.CreateNoWindow = true;
            args.ForEach(a => startScript.StartInfo.ArgumentList.Add(a));
            startScript.Start();
        }
    }
}
