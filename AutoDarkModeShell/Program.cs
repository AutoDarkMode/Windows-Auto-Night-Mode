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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AutoDarkModeSvc.Communication;
using Sharprompt;

namespace AutoDarkModeComms
{
    class Program
    {
        private static Version Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version;

        public const string QuitShell = "QuitShell";
        public const string Custom = "CustomCommand";

        static void Main(string[] args)
        {
            Console.WriteLine($"Auto Dark Mode Shell version {Version.Major}.{Version.Minor}");
            IMessageClient client = new PipeClient();
            if (args.Length > 0)
            {
                if (args.ToList().Contains("--and-launch-service"))
                {
                    using Mutex mutex = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
                    if (mutex.WaitOne(1))
                    {
                        Console.WriteLine($"attempting to start service");
                        mutex.ReleaseMutex();
                        using Process svc = new();
                        svc.StartInfo.UseShellExecute = false;
                        svc.StartInfo.FileName = GetExecutionPathService();
                        _ = svc.Start();
                    }
                }
                int timeoutDefault = 10;
                Console.WriteLine(args[0]);
                if (args.Length == 2)
                {
                    Console.WriteLine($"custom timeout: {args[1]}s");
                    bool success = int.TryParse(args[1], out timeoutDefault);
                    if (!success) timeoutDefault = 10;
                }
                Console.WriteLine($"Result: {client.SendMessageAndGetReply(args[0], timeoutSeconds: timeoutDefault)}");
                Console.WriteLine("Please check service.log for more details");
                Environment.Exit(0);
            }
            var flags = BindingFlags.Static | BindingFlags.Public;
            List<string> fields = typeof(Command).GetFields(flags)
                .Where(p => p.IsDefined(typeof(IncludableAttribute)))
                .Select(f => $"{f.Name} ({(string)typeof(Command).GetField(f.Name).GetValue(null)})")
                .ToList();
            fields.Add(Custom);
            fields.Add(QuitShell);
            string selection = "";
            do
            {
                selection = Prompt.Select("Select a command", fields);
                if (selection == Custom)
                {
                    selection = Prompt.Input<string>("Enter command");
                    Console.WriteLine($"Result: {client.SendMessageAndGetReply(selection, timeoutSeconds: 15)}");
                }
                else if (selection != QuitShell)
                {
                    selection = selection.Split("(")[0].Trim();
                    selection = (string)typeof(Command).GetField(selection).GetValue(null);
                    Console.WriteLine($"Result: {client.SendMessageAndGetReply(selection, timeoutSeconds: 15)}");
                    Console.WriteLine("Please check service.log for more details");
                }
            }
            while (selection != QuitShell);
        }
        public static string GetExecutionPathService()
        {
            var assemblyLocation = AppContext.BaseDirectory;
            var executableName = Path.DirectorySeparatorChar + "AutoDarkModeSvc.exe";
            var executablePath = Path.GetDirectoryName(assemblyLocation);
            return Path.Combine(executablePath + executableName);
        }
    }
}
