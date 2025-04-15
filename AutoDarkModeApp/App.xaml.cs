using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Models;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static Window MainWindow { get; set; } = null!;

    public App()
    {
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

            // Views and ViewModels
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
            services.AddTransient<AppsViewModel>();
            services.AddTransient<AppsPage>();
            services.AddTransient<SwitchModesViewModel>();
            services.AddTransient<SwitchModesPage>();
            services.AddTransient<WallpaperPickerViewModel>();
            services.AddTransient<WallpaperPickerPage>();
            services.AddTransient<TimeViewModel>();
            services.AddTransient<TimePage>();
            services.AddTransient<MainViewModel>();

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

        // Autostart logic
        var localSettings = App.GetService<ILocalSettingsService>();
        var isFirstRun = await localSettings.ReadSettingAsync<bool>("FirstRun");

        if (isFirstRun)
        {
            // Enable autostart on first run
            AutostartHandler.EnableAutoStart(null);

            // Perform other first-run tasks if needed
            await localSettings.SaveSettingAsync("FirstRun", false);
        }
        else
        {
            // Ensure autostart is valid on subsequent runs
            AutostartHandler.EnsureAutostart(null);
        }

        var mainViewModel = App.GetService<MainViewModel>();
        MainWindow = new MainWindow(mainViewModel);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }

    private static async Task SetApplicationLanguageAsync()
    {
        var localSettings = App.GetService<ILocalSettingsService>();
        var language = await localSettings.ReadSettingAsync<string>("Language");
        if (language != null)
        {
            language = language.Replace("\"", "");
            switch (language)
            {
                case "English (English)":
                    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-us";
                    break;
                case "Français (French)":
                    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "fr";
                    break;
                case "日本語 (Japanese)":
                    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "ja";
                    break;
                case "简体中文 (Simplified Chinese)":
                    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "zh-hans";
                    break;
            }
        }
    }
}
