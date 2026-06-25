using System.Diagnostics;
using AutoDarkModeApp.Services;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeApp.Views;
using AutoDarkModeComms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    /// <summary>
    /// Retrieves a registered service of the specified type from the application's dependency injection container.
    /// </summary>
    /// <remarks>Use this method to access services configured in the application's dependency injection
    /// container. Ensure that the service type is registered in the ConfigureServices method of App.xaml.cs before
    /// calling this method.</remarks>
    /// <typeparam name="T">The type of service to retrieve. Must be a reference type and registered in the application's service
    /// collection.</typeparam>
    /// <returns>An instance of the requested service type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the requested service type <typeparamref name="T"/> is not registered in the application's service
    /// collection.</exception>
    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new InvalidOperationException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static MainWindow MainWindow { get; private set; } = null!;

    public App()
    {
        CheckAppMutex();
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                    services.AddSingleton<IFileService, FileService>();

                    services.AddSingleton<IActivationService, ActivationService>();
                    services.AddSingleton<ICloseService, CloseService>();
                    services.AddSingleton<IPageService, PageService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    services.AddSingleton<IErrorService, ErrorService>();
                    services.AddSingleton<IGeolocatorService, GeolocatorService>();

                    // Window
                    // NOTE: The MainWindow is registered as a singleton because we only need one instance.
                    services.AddSingleton<MainWindow>();

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
                }
            )
            .Build();

        UnhandledException += App_UnhandledException;
    }

    public static void CheckAppMutex()
    {
        if (!Mutex.WaitOne(TimeSpan.FromMilliseconds(50), false) && !Debugger.IsAttached)
        {
            var processes = Process.GetProcessesByName("AutoDarkModeApp").Where(p => p.Id != Environment.ProcessId).ToList();
            if (processes.Count > 0)
            {
                Helpers.WindowHelper.BringProcessToFront(processes[0]);
                App.Current.Exit();
            }
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Handle JumpListCommand
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Length > 1)
        {
            new PipeClient().SendMessageAndGetReply(arguments[1]);
            App.Current.Exit();
            return;
        }

        // Set App and Svc language
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = await LanguageHelper.GetDefaultLanguageAsync();
        await Task.Run(() =>
        {
            var builder = AdmConfigBuilder.Instance();
            builder.Load();
            builder.Config.Tunable.UICulture = LanguageHelper.SelectedLanguageCode; // For Svc and other services that need to know the UI culture
            builder.Save();
        });

        // NOTE: Here we use the DI container to get the MainWindow and set it as a static property, which not only conforms to the standard, but also facilitates other places to access the MainWindow.
        MainWindow = GetService<MainWindow>();
        MainWindow.Closed += async (s, e) => await GetService<ICloseService>().CloseAsync();

        await GetService<IActivationService>().ActivateAsync(args);

        // NOTE: The MainWindow must be activated (i.e. made visible) in the ActivationService, not here in App.xaml.cs, because there are navigation events and adjustment of window position and size.
    }
}
