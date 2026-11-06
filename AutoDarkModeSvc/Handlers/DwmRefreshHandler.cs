using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers.ThemeFiles;
using NLog;
using static AutoDarkModeLib.IThemeManager2.Flags;

namespace AutoDarkModeSvc.Handlers;

internal sealed partial class DwmRefreshHandler
{
    // ### 1. Fields and shared state ###

    private static readonly DwmRefreshHandler _instance = new();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly GlobalState state = GlobalState.Instance();
    private static readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();

    private const int TIMEOUT_MS = 5000;
    private const int HWND_BROADCAST = 0xffff;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_THEMECHANGED = 0x031A;
    private const int SMTO_ABORTIFHUNG = 0x0002;
    //private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

    private BlockingCollection<DwmRefreshEventArgs> Queue { get; }
    private Thread Worker { get; set; }
    private CancellationTokenSource Cancellation { get; } = new();

    private int _refreshInProgress;
    private long _nextExecutionTicks;

    // ### 2. Native interop ###

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SendMessageTimeoutW",
                   StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    // ### 3. Construction and lifecycle ###

    private DwmRefreshHandler()
    {
        Queue = [];
        StartWorker();
    }

    public static void Shutdown()
    {
        _instance.Dispose();
    }

    private void Dispose()
    {
        Queue.CompleteAdding();

        Cancellation.Cancel();

        if (Worker.IsAlive) Worker.Join();

        Queue.Dispose();
        Cancellation.Dispose();
        Logger.Debug("dwm management: refresh handler stopped");
    }

    private void StartWorker()
    {
        Worker = new Thread(() =>
        {
            try
            {
                foreach (DwmRefreshEventArgs e in Queue.GetConsumingEnumerable(Cancellation.Token))
                {

                    while (true)
                    {
                        long now = DateTime.UtcNow.Ticks;
                        long deadline = Volatile.Read(ref _nextExecutionTicks);
                        long remainingMs = (deadline - now) / TimeSpan.TicksPerMillisecond;
                        if (remainingMs > 0)
                        {
                            Thread.Sleep((int)remainingMs);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // we consider a refresh "done" as soon as the wait time is over
                    // because we don't know at which stage the refresh is
                    // and it's better to allow enqueuing a new one instead of
                    // potentially having UI components not update
                    Volatile.Write(ref _refreshInProgress, 0);
                    Volatile.Write(ref _nextExecutionTicks, 0);

                    try
                    {
                        if (e.Type > DwmRefreshType.Standard) RefreshDwmViaColorization();
                        else Broadcast();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"dwm management: refresh failed, source {Enum.GetName(e.RefreshSource)}");
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

    // ### 4. Public API ###

    public static void Enqueue(DwmRefreshEventArgs e)
    {

        long now = DateTime.UtcNow.Ticks;
        long delayTicks = TimeSpan.FromMilliseconds(e.Delay).Ticks;
        long newDeadline = now + delayTicks;

        if (Interlocked.CompareExchange(ref _instance._refreshInProgress, 1, 0) == 0)
        {
            Logger.Debug($"dwm management: enqueuing new dwm refresh{(e.Delay > 0 ? $" with a delay of {e.Delay} millis" : "")} from source {Enum.GetName(e.RefreshSource)}");
            Volatile.Write(ref _instance._nextExecutionTicks, newDeadline);
            _instance.Queue.Add(e);
        }
        else
        {
            long currentDeadline = Volatile.Read(ref _instance._nextExecutionTicks);
            double remainingMs = (currentDeadline - now) / (double)TimeSpan.TicksPerMillisecond;
            //remainingMs = Math.Max(0, remainingMs);

            Logger.Debug($"dwm management: combining refresh request from source {Enum.GetName(e.RefreshSource)} with active pending refresh in queue");

            // if the currently requested delay is larger than our wait time, we re-set the deadline to the last requested delay
            if (e.Delay >= remainingMs)
            {
                Interlocked.CompareExchange(ref _instance._nextExecutionTicks, newDeadline, currentDeadline);
            }
        }
    }

    // ### 5. Private refresh helpers ###

    private static void Broadcast()
    {
        Logger.Info("dwm management: starting broadcast");
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

            if (result.Equals(UIntPtr.Zero))
            {
                var code = Marshal.GetLastWin32Error();
                Logger.Error("dwm management: broadcast failed while broadcasting hwnd message wm_settingchange", code);
            }

            SendMessageTimeout(
                new IntPtr(HWND_BROADCAST),
                WM_THEMECHANGED,
                UIntPtr.Zero,
                null,
                SMTO_ABORTIFHUNG,
                TIMEOUT_MS,
                out result);

            if (result.Equals(UIntPtr.Zero))
            {
                var code = Marshal.GetLastWin32Error();
                Logger.Error("dwm management: broadcast failed while broadcasting hwnd message wm_themechanged", code);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "dwm management: could not refresh dwm");
        }
        Logger.Info("dwm management: refresh complete");
    }

    private static ThemeFile PrepareUnmanagedDwmRefresh()
    {
        state.ManagedThemeFile.SyncWithActiveTheme(patch: false, keepDisplayNameAndGuid: false, logging: true);
        ThemeFile unmanagedTarget = state.InternalTheme switch
        {
            Theme.Dark => new(builder.Config.WindowsThemeMode.DarkThemePath),
            _ => new(builder.Config.WindowsThemeMode.LightThemePath),
        };
        unmanagedTarget.Load();
        return unmanagedTarget;
    }

    private static void RefreshDwmViaColorization()
    {
        ThemeFile unmanagedTarget;
        try
        {
            // prepare theme
            ThemeFile dwmRefreshTheme = new(Helper.DwmRefreshThemePath);
            bool managed = !builder.Config.WindowsThemeMode.Enabled;
            if (!managed)
            {
                unmanagedTarget = PrepareUnmanagedDwmRefresh();
                try
                {
                    dwmRefreshTheme.SetContentAndParse(unmanagedTarget.ThemeFileContent);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "dwm management: could not load unmanaged target for refresh, forcing with managed theme");
                    dwmRefreshTheme.SetContentAndParse(state.ManagedThemeFile.ThemeFileContent);
                }
            }
            else
            {
                dwmRefreshTheme.SetContentAndParse(state.ManagedThemeFile.ThemeFileContent);
            }
            dwmRefreshTheme.RefreshGuid();
            dwmRefreshTheme.DisplayName = "DwmRefreshTheme";

            // get current accent color
            string currentColorization = RegistryHandler.GetAccentColor().Replace("#", "0X");
            string lastColorizationDigitString = currentColorization[^1].ToString();
            int lastColorizationDigit = int.Parse(lastColorizationDigitString, System.Globalization.NumberStyles.HexNumber);

            // modify last digit
            if (lastColorizationDigit >= 9) lastColorizationDigit--;
            else lastColorizationDigit++;
            string newColorizationColor = currentColorization[..^1] + lastColorizationDigit.ToString("X");

            // update theme
            dwmRefreshTheme.VisualStyles.ColorizationColor = (newColorizationColor, dwmRefreshTheme.VisualStyles.ColorizationColor.Item2);
            dwmRefreshTheme.VisualStyles.AutoColorization = ("0", dwmRefreshTheme.VisualStyles.AutoColorization.Item2);
            dwmRefreshTheme.Save();

            List<ThemeApplyFlags> flagList = [ThemeApplyFlags.IgnoreBackground, ThemeApplyFlags.IgnoreCursor, ThemeApplyFlags.IgnoreDesktopIcons, ThemeApplyFlags.IgnoreSound, ThemeApplyFlags.IgnoreScreensaver];
            Logger.Debug($"dwm management: temporarily setting accent color to {dwmRefreshTheme.VisualStyles.ColorizationColor.Item1} from {currentColorization}");
            ThemeHandler.Apply(dwmRefreshTheme.ThemeFilePath, true, null, flagList);
            Thread.Sleep(1000);

            if (managed)
            {
                ThemeHandler.Apply(state.ManagedThemeFile.ThemeFilePath, true, null, flagList);
            }
            if (!managed)
            {
                ThemeHandler.ApplyUnmanagedTheme(state.InternalTheme);
            }
            Logger.Info("dwm management: full colorization refresh performed by theme handler");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "dwm management: could not perform full colorization refresh due to malformed colorization string: ");
        }
    }
}
