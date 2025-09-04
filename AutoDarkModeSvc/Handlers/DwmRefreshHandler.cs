using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeSvc.Events;
using NLog;

namespace AutoDarkModeSvc.Handlers;
internal sealed partial class DwmRefreshHandler
{
    private static readonly DwmRefreshHandler _instance = new();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private BlockingCollection<SwitchEventArgs> Queue { get; }
    private Thread Worker { get; set; }
    private CancellationTokenSource Cancellation { get; } = new();

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SendMessageTimeoutW",
                   StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string? lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    private const int TIMEOUT_MS = 5000;
    private const int HWND_BROADCAST = 0xffff;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_THEMECHANGED = 0x031A;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    private DwmRefreshHandler()
    {
        Queue = [];
        WorkerManager();
    }

    private void WorkerManager()
    {
        Worker = new Thread(() =>
        {
            try
            {
                foreach (SwitchEventArgs e in Queue.GetConsumingEnumerable(Cancellation.Token))
                {
                    try
                    {
                        BroadcastMessages();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "dwm management: refresh failed");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Trace("dwm management: queue worker cancellation token received");
            }
        })
        {
            Name = "DWMRefreshWorker",
            IsBackground = true
        };

        Worker.SetApartmentState(ApartmentState.STA);
        Worker.Start();
    }

    public static void Enqueue(SwitchEventArgs e)
    {
        Logger.Debug("dwm management: enqueuing new dwm refresh");
        _instance.Queue.Add(e);
    }

    public static void Shutdown()
    {
        _instance.Dispose();
    }

    private void Dispose()
    {
        Queue.CompleteAdding();

        Cancellation.Cancel();

        if (Worker.IsAlive)
        {
            Worker.Join();
        }

        Queue.Dispose();
        Cancellation.Dispose();
        Logger.Debug("dwm management: refresh handler stopped");
    }

    private static void BroadcastMessages()
    {
        Logger.Info("dwm management: starting refresh");
        try
        {
            UIntPtr result;

            SendMessageTimeout(
                new IntPtr(HWND_BROADCAST),
                WM_SETTINGCHANGE,
                UIntPtr.Zero,
                "ImmersiveColorSet",
                SMTO_ABORTIFHUNG,
                TIMEOUT_MS,
                out result);

            if (result.Equals(IntPtr.Zero))
            {
                var code = Marshal.GetLastWin32Error();
                Logger.Error("dwm management: refresh failed while broadcasting hwnd message wm_settingchange", code);
            }

            SendMessageTimeout(
                new IntPtr(HWND_BROADCAST),
                WM_THEMECHANGED,
                UIntPtr.Zero,
                null,
                SMTO_ABORTIFHUNG,
                TIMEOUT_MS,
                out result);

            if (result.Equals(IntPtr.Zero))
            {
                var code = Marshal.GetLastWin32Error();
                Logger.Error("dwm management: refresh failed while broadcasting hwnd message wm_themechanged", code);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "dwm management: could not refresh dwm", ex);
        }
        Logger.Info("dwm management: refresh complete");
    }
}