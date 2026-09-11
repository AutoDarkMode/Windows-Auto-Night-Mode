using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoDarkModeLib;
using AutoDarkModeSvc.Core;
using AutoDarkModeSvc.Events;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Interfaces;
using AutoDarkModeSvc.Modules;
using Microsoft.Win32;

namespace AutoDarkModeSvc.Governors;

public class PowerGovernor : IAutoDarkModeGovernor
{
    public Governor Type => Governor.Power;
    private IAutoDarkModeModule Master { get; }
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private GlobalState state = GlobalState.Instance();
    private bool init = true;

    private AdmConfigBuilder builder = AdmConfigBuilder.Instance();
    private readonly object powerWatcherLock = new();
    private CancellationTokenSource powerWatcherCancellation;
    private Task powerWatcherTask;

    private const uint RegNotifyChangeLastSet = 0x00000004;
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, uint notifyFilter, IntPtr hEvent, bool asynchronous);

    public PowerGovernor(IAutoDarkModeModule master)
    {
        Master = master;
    }

    public GovernorEventArgs Run()
    {
        // Reload config to pick up any changes made directly to the config file
        builder.Load();

        if (init)
        {
            init = false;
        }

        return new(false, new(SwitchSource.PowerModule, state.PowerState.Requested));
    }

    private void UpdatePowerState()
    {
        try
        {
            var energySaverEnabled = RegistryHandler.IsEnergySaverEnabled();
            if (energySaverEnabled)
            {
                state.PowerState.Requested = Theme.Dark;
            }
            else
            {
                state.PowerState.Requested = Theme.Light;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating power state");
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void MonitorPowerRegistry(CancellationToken cancellationToken)
    {
        try
        {
            using RegistryKey powerKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
            if (powerKey is null)
            {
                Logger.Error("Could not open the power registry key for native change notifications.");
                return;
            }

            using AutoResetEvent registryChanged = new(false);
            WaitHandle[] waitHandles = [registryChanged, cancellationToken.WaitHandle];

            while (!cancellationToken.IsCancellationRequested)
            {
                int result = RegNotifyChangeKeyValue(
                    powerKey.Handle.DangerousGetHandle(),
                    false,
                    RegNotifyChangeLastSet,
                    registryChanged.SafeWaitHandle.DangerousGetHandle(),
                    true);

                if (result != 0)
                {
                    Logger.Error("Failed to register the native power registry notification. Error code: {ErrorCode}", result);
                    return;
                }

                if (WaitHandle.WaitAny(waitHandles) == 0)
                {
                    UpdatePowerState();
                    Master.Fire(this);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Power governor native registry monitor stopped unexpectedly.");
        }
    }

    public void EnableHook()
    {
        lock (powerWatcherLock)
        {
            if (powerWatcherTask == null)
            {
                powerWatcherCancellation = new CancellationTokenSource();
                powerWatcherTask = Task.Run(() => MonitorPowerRegistry(powerWatcherCancellation.Token));
                Logger.Info("Power governor native registry monitor enabled.");
            }
        }

        UpdatePowerState();
    }

    public void DisableHook()
    {
        CancellationTokenSource cancellation;
        Task watcherTask;

        lock (powerWatcherLock)
        {
            cancellation = powerWatcherCancellation;
            watcherTask = powerWatcherTask;
            powerWatcherCancellation = null;
            powerWatcherTask = null;
        }

        try
        {
            cancellation?.Cancel();
            watcherTask?.Wait();
            Logger.Info("Power governor native registry monitor disabled.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to disable power governor hook.");
        }
        finally
        {
            cancellation?.Dispose();
        }
    }
}
