#region copyright
// TODO: Should we reduced copyright header? Made it more concise while keeping all important info
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion

using System.Collections.Concurrent;
using System.Security.Principal;
using System.Timers;
using AutoDarkModeLib;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace AutoDarkModeApp.Utils.Handlers;

public static class StateUpdateHandler
{
    private static readonly ConcurrentBag<ElapsedEventHandler> _delegatesTimer = new();
    private static readonly ConcurrentBag<FileSystemEventHandler> _delegatesConfigWatcher = new();
    private static readonly ConcurrentBag<FileSystemEventHandler> _delegatesScriptConfigWatcher = new();
    private static readonly DispatcherQueueTimer _debounceTimer;
    private static Action? _debounceAction;

    public static SecurityIdentifier SID => WindowsIdentity.GetCurrent().User!;

    static StateUpdateHandler()
    {
        try
        {
            PostponeRefreshTimer.Interval = 2000;
            ConfigWatcher.NotifyFilter = NotifyFilters.LastWrite;
            ScriptConfigWatcher.NotifyFilter = NotifyFilters.LastWrite;

            _debounceTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _debounceTimer.Interval = TimeSpan.FromMilliseconds(100);
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceAction?.Invoke();
                _debounceTimer.Stop();
            };
        }
        catch (Exception ex)
        {
            throw new ArgumentException(ex.Message);
        }
    }

    private static FileSystemWatcher ConfigWatcher { get; } = new()
    {
        Path = AdmConfigBuilder.ConfigDir,
        Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath)
    };

    private static FileSystemWatcher ScriptConfigWatcher { get; } = new()
    {
        Path = AdmConfigBuilder.ConfigDir,
        Filter = Path.GetFileName(AdmConfigBuilder.ScriptConfigPath)
    };

    private static System.Timers.Timer PostponeRefreshTimer { get; } = new();

    public static void ClearAllEvents()
    {
        ClearEventHandlers(_delegatesTimer, eh => PostponeRefreshTimer.Elapsed -= eh);
        ClearEventHandlers(_delegatesConfigWatcher, eh => ConfigWatcher.Changed -= eh);
        ClearEventHandlers(_delegatesScriptConfigWatcher, eh => ScriptConfigWatcher.Changed -= eh);
    }

    private static void ClearEventHandlers<T>(ConcurrentBag<T> handlers, Action<T> removeAction)
    {
        foreach (var handler in handlers)
        {
            removeAction(handler);
        }
        handlers.Clear();
    }

    public static void StartScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => ScriptConfigWatcher.EnableRaisingEvents = true);

    public static void StopScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => ScriptConfigWatcher.EnableRaisingEvents = false);

    public static void StartPostponeTimer() => PostponeRefreshTimer.Start();

    public static void StopPostponeTimer() => PostponeRefreshTimer.Stop();

    public static void StartConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => ConfigWatcher.EnableRaisingEvents = true);

    public static void StartConfigWatcherWithoutEvents()
    {
        ClearEventHandlers(_delegatesConfigWatcher, eh => ConfigWatcher.Changed -= eh);
        SafetyExtensions.IgnoreExceptions(() => ConfigWatcher.EnableRaisingEvents = true);
    }

    public static void StopConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => ConfigWatcher.EnableRaisingEvents = false);

    public static void StopConfigWatcherWithoutEvents()
    {
        SafetyExtensions.IgnoreExceptions(() => ConfigWatcher.EnableRaisingEvents = false);
        ClearEventHandlers(_delegatesConfigWatcher, eh => ConfigWatcher.Changed -= eh);
    }

    public static event FileSystemEventHandler OnScriptConfigUpdate
    {
        add => ScriptConfigWatcher.Changed += value;
        remove => ScriptConfigWatcher.Changed -= value;
    }

    public static event FileSystemEventHandler OnConfigUpdate
    {
        add => ConfigWatcher.Changed += value;
        remove => ConfigWatcher.Changed -= value;
    }

    public static void AddDebounceEventOnConfigUpdate(Action action)
    {
        _debounceAction = action;
        OnConfigUpdate += DebounceAction;
    }

    public static event ElapsedEventHandler OnPostponeTimerTick
    {
        add => PostponeRefreshTimer.Elapsed += value;
        remove => PostponeRefreshTimer.Elapsed -= value;
    }

    private static void DebounceAction(object sender, FileSystemEventArgs e)
    {
        if (!_debounceTimer.IsRunning)
        {
            _debounceTimer.Start();
        }
    }

    public static void Dispose()
    {
        ConfigWatcher.Dispose();
        ScriptConfigWatcher.Dispose();
        PostponeRefreshTimer.Dispose();
        _debounceTimer.Stop();
    }

    private static class SafetyExtensions
    {
        public static bool IgnoreExceptions(Action action, ILogger? logger = null, Type? exceptionToIgnore = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (exceptionToIgnore == null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
            {
                logger?.LogInformation(ex, ex.Message);
                return false;
            }
        }
    }
}