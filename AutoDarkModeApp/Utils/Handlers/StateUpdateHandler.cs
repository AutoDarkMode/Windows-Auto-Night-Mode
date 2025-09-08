#region copyright
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion

using System.Security.Principal;
using System.Timers;
using AutoDarkModeLib;
using Microsoft.UI.Dispatching;

namespace AutoDarkModeApp.Utils.Handlers;

internal static class StateUpdateHandler
{
    private static readonly List<FileSystemEventHandler> _delegatesConfigWatcher = [];
    private static readonly List<FileSystemEventHandler> _delegatesScriptConfigWatcher = [];
    private static readonly List<ElapsedEventHandler> _delegatesTimer = [];

    private static readonly DispatcherQueueTimer _debounceTimer;
    private static Action? _debounceAction;

    public static SecurityIdentifier SID => WindowsIdentity.GetCurrent().User!;

    static StateUpdateHandler()
    {
        PostponeRefreshTimer.Interval = 2000;

        try
        {
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

    private static FileSystemWatcher? ConfigWatcher
    {
        get
        {
            if (!Directory.Exists(AdmConfigBuilder.ConfigDir))
            {
                return null;
            }

            return new FileSystemWatcher
            {
                Path = AdmConfigBuilder.ConfigDir,
                Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath),
                NotifyFilter = NotifyFilters.LastWrite,
            };
        }
    }



    private static FileSystemWatcher? ScriptConfigWatcher
    {
        get
        {
            if (!File.Exists(AdmConfigBuilder.ScriptConfigPath))
            {
                return null;
            }

            return new FileSystemWatcher
            {
                Path = AdmConfigBuilder.ConfigDir,
                Filter = Path.GetFileName(AdmConfigBuilder.ScriptConfigPath),
                NotifyFilter = NotifyFilters.LastWrite,
            };
        }
    }

    private static System.Timers.Timer PostponeRefreshTimer { get; } = new();

    public static void ClearAllEvents()
    {
        ClearEventHandlers(_delegatesTimer, eh => PostponeRefreshTimer.Elapsed -= eh);
        ClearEventHandlers(_delegatesConfigWatcher, eh => ConfigWatcher?.Changed -= eh);
        ClearEventHandlers(_delegatesScriptConfigWatcher, eh => ScriptConfigWatcher?.Changed -= eh);
    }

    private static void ClearEventHandlers<T>(List<T> handlers, Action<T> removeAction)
    {
        foreach (var handler in handlers)
        {
            removeAction(handler);
        }
        handlers.Clear();
    }

    public static void StartConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => ConfigWatcher?.EnableRaisingEvents = true);

    public static void StopConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => ConfigWatcher?.EnableRaisingEvents = false);

    public static void StartScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => ScriptConfigWatcher?.EnableRaisingEvents = true);

    public static void StopScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => ScriptConfigWatcher?.EnableRaisingEvents = false);

    public static void StartPostponeTimer() => PostponeRefreshTimer.Start();

    public static void StopPostponeTimer() => PostponeRefreshTimer.Stop();

    private static class SafetyExtensions
    {
        public static bool IgnoreExceptions(Action action, Type? exceptionToIgnore = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (exceptionToIgnore == null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }

    public static event FileSystemEventHandler OnConfigUpdate
    {
        add
        {
            ConfigWatcher.Changed += value;
            _delegatesConfigWatcher.Add(value);
        }
        remove
        {
            ConfigWatcher.Changed -= value;
            _delegatesConfigWatcher.Remove(value);
        }
    }

    public static event FileSystemEventHandler OnScriptConfigUpdate
    {
        add
        {
            ScriptConfigWatcher.Changed += value;
            _delegatesScriptConfigWatcher.Add(value);
        }
        remove
        {
            ScriptConfigWatcher.Changed -= value;
            _delegatesScriptConfigWatcher.Remove(value);
        }
    }

    public static event ElapsedEventHandler OnPostponeTimerTick
    {
        add
        {
            PostponeRefreshTimer.Elapsed += value;
            _delegatesTimer.Add(value);
        }
        remove
        {
            PostponeRefreshTimer.Elapsed -= value;
            _delegatesTimer.Remove(value);
        }
    }

    public static void AddDebounceEventOnConfigUpdate(Action action)
    {
        _debounceAction = action;
        OnConfigUpdate += DebounceAction;
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
}
