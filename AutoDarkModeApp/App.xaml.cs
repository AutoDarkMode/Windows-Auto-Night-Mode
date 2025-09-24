using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Models;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.Utils;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    public static Mutex Mutex { get; private set; } = new Mutex(false, "821abd85-51af-4379-826c-41fb68f0e5c5");

    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static void CheckAppMutex()
    {
        if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(50), false) && !Debugger.IsAttached)
        {
            List<Process> processes = [.. Process.GetProcessesByName("AutoDarkModeApp")];
            if (processes.Count > 0)
            {
                Helpers.WindowHelper.BringProcessToFront(processes[0]);
                Environment.Exit(-1);
            }
        }
    }

    public static Window MainWindow { get; set; } = null!;

    public App()
    {
        CheckAppMutex();

        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IFileService, FileService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<IErrorService, ErrorService>();
            services.AddSingleton<IGeolocatorService, GeolocatorService>();

            // Views and ViewModels
            services.AddTransient<ThemePickerViewModel>();
            services.AddTransient<ThemePickerPage>();
            services.AddTransient<CursorsViewModel>();
            services.AddTransient<CursorsPage>();
            services.AddTransient<ColorizationViewModel>();
            services.AddTransient<ColorizationPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<AboutPage>();
            services.AddTransient<DonationViewModel>();
            services.AddTransient<DonationPage>();
            services.AddTransient<ScriptsViewModel>();
            services.AddTransient<ScriptsPage>();
            services.AddTransient<PersonalizationViewModel>();
            services.AddTransient<PersonalizationPage>();
            services.AddTransient<SystemAreasViewModel>();
            services.AddTransient<SystemAreasPage>();
            services.AddTransient<ConditionsViewModel>();
            services.AddTransient<ConditionsPage>();
            services.AddTransient<HotkeysViewModel>();
            services.AddTransient<HotkeysPage>();
            services.AddTransient<WallpaperPickerViewModel>();
            services.AddTransient<WallpaperPickerPage>();
            services.AddTransient<TimeViewModel>();
            services.AddTransient<TimePage>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await SetApplicationLanguageAsync();

        var navigationService = App.GetService<INavigationService>();
        MainWindow = new MainWindow(navigationService);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }

    private static async Task SetApplicationLanguageAsync()
    {
        var localSettings = App.GetService<ILocalSettingsService>();
        var language = await localSettings.ReadSettingAsync<string>("SelectedLanguageCode");
        if (!string.IsNullOrEmpty(language))
        {
            Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;
        }
    }
}
