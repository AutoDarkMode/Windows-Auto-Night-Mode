using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoDarkModeSvc.Communication;
using Sharprompt;

namespace AutoDarkModeComms
{
    class Program
    {
        public const string QuitShell = "QuitShell";
        static void Main(string[] args)
        {
            Console.WriteLine("Auto Dark Mode Shell version 1.0");
            ICommandClient client = new ZeroMQClient(Address.DefaultPort);
            if (args.Length > 0)
            {
                List<string> argsList = new();
                Console.WriteLine($"Result: {client.SendMessageAndGetReply(argsList[0])}");
                Console.WriteLine("Please check service.log for more details");
                Environment.Exit(0);
            }
            var flags = BindingFlags.Static | BindingFlags.Public;
            List<string> fields = typeof(Command).GetFields(flags)
                .Where(p => p.IsDefined(typeof(IncludableAttribute)))
                .Select(f => $"{f.Name} ({(string)typeof(Command).GetField(f.Name).GetValue(null)})")
                .ToList();
            fields.Add(QuitShell);
            string selection = "";
            do
            {
                selection = Prompt.Select("Select a command", fields);
                selection = selection.Split("(")[0].Trim();
                if (selection != QuitShell)
                {
                    selection = (string)typeof(Command).GetField(selection).GetValue(null);
                    Console.WriteLine($"Result: {client.SendMessageAndGetReply(selection)}");
                    Console.WriteLine("Please check service.log for more details");
                }
            }
            while (selection != QuitShell);

        }
    }
}
