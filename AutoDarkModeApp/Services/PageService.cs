﻿using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = [];

    public PageService()
    {
        Configure<TimeViewModel, TimePage>();
        Configure<ConditionsViewModel, ConditionsPage>();
        Configure<HotkeysViewModel, HotkeysPage>();
        Configure<SystemAreasViewModel, SystemAreasPage>();
        Configure<PersonalizationViewModel, PersonalizationPage>();
        Configure<ScriptsViewModel, ScriptsPage>();
        Configure<DonationViewModel, DonationPage>();
        Configure<AboutViewModel, AboutPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<WallpaperPickerViewModel, WallpaperPickerPage>();
        Configure<ColorizationViewModel, ColorizationPage>();
        Configure<CursorsViewModel, CursorsPage>();
        Configure<ThemePickerViewModel, ThemePickerPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType!;
    }

    private void Configure<VM, V>()
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
        }
    }
}
