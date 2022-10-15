using System.Diagnostics;

namespace AutoDarkModeApp.Handlers;

public class ProcessHandler
{
    public static void StartProcessByProcessInfo(string message)
    {
        Process.Start(new ProcessStartInfo(message)
        {
            UseShellExecute = true,
            Verb = "open"
        });
    }
}