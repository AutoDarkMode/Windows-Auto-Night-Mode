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
    private static readonly Lock _lock = new();
    private static FileSystemWatcher? _configWatcher;
    private static FileSystemWatcher? _scriptConfigWatcher;
    private static readonly System.Timers.Timer _postponeRefreshTimer;

    private static readonly List<FileSystemEventHandler> _delegatesConfigWatcher = [];
    private static readonly List<FileSystemEventHandler> _delegatesScriptConfigWatcher = [];
    private static readonly List<ElapsedEventHandler> _delegatesTimer = [];

    private static readonly DispatcherQueueTimer? _debounceTimer;
    private static Action? _debounceAction;

    public static SecurityIdentifier SID => WindowsIdentity.GetCurrent().User!;

    static StateUpdateHandler()
    {
        _postponeRefreshTimer = new System.Timers.Timer(2000);

        try
        {
            var queue = DispatcherQueue.GetForCurrentThread();
            if (queue != null)
            {
                _debounceTimer = queue.CreateTimer();
                _debounceTimer.Interval = TimeSpan.FromMilliseconds(100);
                _debounceTimer.Tick += OnDebounceTimerTick;
            }
        }
        catch
        {
            // Ignore if DispatcherQueue is not available
        }
    }

    private static void OnDebounceTimerTick(object? sender, object e)
    {
        _debounceAction?.Invoke();
        _debounceTimer?.Stop();
    }

    private static FileSystemWatcher? GetConfigWatcher()
    {
        if (_configWatcher != null)
            return _configWatcher;

        lock (_lock)
        {
            if (_configWatcher != null)
                return _configWatcher;

            if (!Directory.Exists(AdmConfigBuilder.ConfigDir))
                return null;

            try
            {
                _configWatcher = new FileSystemWatcher
                {
                    Path = AdmConfigBuilder.ConfigDir,
                    Filter = Path.GetFileName(AdmConfigBuilder.ConfigFilePath),
                    NotifyFilter = NotifyFilters.LastWrite,
                };
            }
            catch
            {
                return null;
            }
        }

        return _configWatcher;
    }

    private static FileSystemWatcher? GetScriptConfigWatcher()
    {
        if (_scriptConfigWatcher != null)
            return _scriptConfigWatcher;

        lock (_lock)
        {
            if (_scriptConfigWatcher != null)
                return _scriptConfigWatcher;

            if (!File.Exists(AdmConfigBuilder.ScriptConfigPath))
                return null;

            try
            {
                _scriptConfigWatcher = new FileSystemWatcher
                {
                    Path = AdmConfigBuilder.ConfigDir,
                    Filter = Path.GetFileName(AdmConfigBuilder.ScriptConfigPath),
                    NotifyFilter = NotifyFilters.LastWrite,
                };
            }
            catch
            {
                return null;
            }
        }

        return _scriptConfigWatcher;
    }

    public static void ClearAllEvents()
    {
        ClearEventHandlers(_delegatesTimer, eh => _postponeRefreshTimer.Elapsed -= eh);
        ClearEventHandlers(_delegatesConfigWatcher, eh => GetConfigWatcher()?.Changed -= eh);
        ClearEventHandlers(_delegatesScriptConfigWatcher, eh => GetScriptConfigWatcher()?.Changed -= eh);
    }

    private static void ClearEventHandlers<T>(List<T> handlers, Action<T> removeAction)
    {
        foreach (var handler in handlers)
        {
            removeAction(handler);
        }
        handlers.Clear();
    }

    public static void StartConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => GetConfigWatcher()?.EnableRaisingEvents = true);

    public static void StopConfigWatcher() => SafetyExtensions.IgnoreExceptions(() => GetConfigWatcher()?.EnableRaisingEvents = false);

    public static void StartScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => GetScriptConfigWatcher()?.EnableRaisingEvents = true);

    public static void StopScriptWatcher() => SafetyExtensions.IgnoreExceptions(() => GetScriptConfigWatcher()?.EnableRaisingEvents = false);

    public static void StartPostponeTimer() => _postponeRefreshTimer.Start();

    public static void StopPostponeTimer() => _postponeRefreshTimer.Stop();

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
            GetConfigWatcher()?.Changed += value;
            _delegatesConfigWatcher.Add(value);
        }
        remove
        {
            GetConfigWatcher()?.Changed -= value;
            _delegatesConfigWatcher.Remove(value);
        }
    }

    public static event FileSystemEventHandler OnScriptConfigUpdate
    {
        add
        {
            GetScriptConfigWatcher()?.Changed += value;
            _delegatesScriptConfigWatcher.Add(value);
        }
        remove
        {
            GetScriptConfigWatcher()?.Changed -= value;
            _delegatesScriptConfigWatcher.Remove(value);
        }
    }

    public static event ElapsedEventHandler OnPostponeTimerTick
    {
        add
        {
            _postponeRefreshTimer.Elapsed += value;
            _delegatesTimer.Add(value);
        }
        remove
        {
            _postponeRefreshTimer.Elapsed -= value;
            _delegatesTimer.Remove(value);
        }
    }

    public static void AddDebounceEventOnConfigUpdate(Action action)
    {
        if(_debounceTimer == null)
        {
            return;
        }

        _debounceAction = action;
        OnConfigUpdate += DebounceAction;
    }

    private static void DebounceAction(object sender, FileSystemEventArgs e)
    {
        if (_debounceTimer?.IsRunning == false)
        {
            _debounceTimer.Start();
        }
    }

    public static void Dispose()
    {
        ClearAllEvents();

        GetConfigWatcher()?.Dispose();
        GetScriptConfigWatcher()?.Dispose();
        _postponeRefreshTimer.Dispose();
        _debounceTimer?.Stop();
    }
}
