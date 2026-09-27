using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeLib;

namespace AutoDarkModeSvc;

/// <summary>Keeps a small WinUI frontend warm; the WinForms service retains all command behavior.</summary>
internal sealed class WinUITrayMenuBridge : IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private Process host;
    private readonly object gate = new();
    private bool disposed;

    public void WarmUp()
    {
        lock (gate)
        {
            if (disposed || (host != null && !host.HasExited)) return;
            host?.Dispose();
            host = Process.Start(new ProcessStartInfo(Helper.ExecutionPathApp)
            {
                Arguments = $"{TrayMenuProtocol.Argument} {Environment.ProcessId}",
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }

    public async Task<int> ShowAsync(TrayMenuModel model)
    {
        WarmUp();
        lock (gate) { if (host != null && !host.HasExited) AllowSetForegroundWindow(host.Id); }
        using var pipe = new NamedPipeClientStream(".", TrayMenuProtocol.PipeName(Environment.ProcessId), PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(5000, lifetime.Token);
        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(model));
        var reply = await reader.ReadLineAsync(lifetime.Token);
        if (!int.TryParse(reply, out var id)) throw new IOException("WinUI tray menu closed without a response.");
        return id;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            lifetime.Cancel();
            try { if (host != null && !host.HasExited) host.Kill(); }
            catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Debug(ex, "tray menu host already stopped"); }
            host?.Dispose();
        }
    }
}
