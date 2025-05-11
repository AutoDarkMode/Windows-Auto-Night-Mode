#region copyright
// TODO: Should we reduced copyright header? Made it more concise while keeping all important info
// Copyright (C) 2025 Auto Dark Mode
// This program is free software under GNU GPL v3.0
#endregion
using AutoDarkModeApp.Contracts.Services;
using Microsoft.Win32;

namespace AutoDarkModeApp.Utils.Handlers;

internal static class CursorCollectionHandler
{
    private const string UserCursorsSchemeKeyPath = @"Control Panel\Cursors\Schemes";
    private const string SystemCursorsSchemeKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes";
    private const string CurrentCursorKeyPath = @"Control Panel\Cursors";

    private static readonly IErrorService _errorService = App.GetService<IErrorService>();

    public static List<string> GetCursors()
    {
        var cursors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var cursorsKeyUser = Registry.CurrentUser.OpenSubKey(UserCursorsSchemeKeyPath);
            if (cursorsKeyUser != null)
            {
                var userCursors = cursorsKeyUser.GetValueNames();
                if (userCursors.Length > 0)
                {
                    foreach (var cursor in userCursors)
                    {
                        cursors.Add(cursor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex,App.MainWindow.Content.XamlRoot, "CursorCollectionHandler.GetCursors");
        }

        try
        {
            using var cursorsKeySystem = Registry.LocalMachine.OpenSubKey(SystemCursorsSchemeKeyPath);
            if (cursorsKeySystem != null)
            {
                var systemCursors = cursorsKeySystem.GetValueNames();
                if (systemCursors.Length > 0)
                {
                    foreach (var cursor in systemCursors)
                    {
                        cursors.Add(cursor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorCollectionHandler.GetCursors");
        }

        return cursors.ToList();
    }

    public static string? GetCurrentCursorScheme()
    {
        try
        {
            using var cursorsKey = Registry.CurrentUser.OpenSubKey(CurrentCursorKeyPath);
            return cursorsKey?.GetValue("") as string;
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorCollectionHandler.GetCurrentCursorScheme");
            return null;
        }
    }

    public static string[] GetCursorScheme(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return [];
        }

        // Try user registry first
        string[] cursorsList = TryGetCursorSchemeFromRegistry(Registry.CurrentUser, UserCursorsSchemeKeyPath, name);

        // If not found in user registry, try system registry
        if (cursorsList.Length == 0)
        {
            cursorsList = TryGetCursorSchemeFromRegistry(Registry.LocalMachine, SystemCursorsSchemeKeyPath, name);
        }

        return cursorsList;
    }

    private static string[] TryGetCursorSchemeFromRegistry(RegistryKey rootKey, string keyPath, string schemeName)
    {
        try
        {
            using var cursorsKey = rootKey.OpenSubKey(keyPath);
            if (cursorsKey != null)
            {
                string? schemeValue = cursorsKey.GetValue(schemeName) as string;
                if (!string.IsNullOrEmpty(schemeValue))
                {
                    return schemeValue.Split(',');
                }
            }
        }
        catch (Exception ex)
        {
            _errorService.ShowErrorMessage(ex, App.MainWindow.Content.XamlRoot, "CursorCollectionHandler.TryGetCursorSchemeFromRegistry");
        }

        return [];
    }
}
