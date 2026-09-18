using System.Diagnostics;
using System.Globalization;
using AutoDarkModeApp.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.UI.StartScreen;

namespace AutoDarkModeApp.Services;

public class ActivationService(ILocalSettingsService localSettingsService, INavigationService navigationService) : IActivationService
{
    public async Task ActivateAsync(object activationArgs)
    {
        AdaptingToLegacyConfigHelper.MigrationSettings(localSettingsService);

        // Navigate to default page
        navigationService.NavigateTo(typeof(TimeViewModel).FullName!);

        // Move window to config position
        MoveWindow();

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Start the service and handle UI flow
        var serviceStartIssued = StartService();
        ContentDialog loadingDialog = null!;

        if (serviceStartIssued)
        {
            await WaitForXamlRootAsync();

            loadingDialog = new ContentDialog
            {
                Title = "LaunchingService".GetLocalized(),
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 32,
                    Children =
                    {
                        new TextBlock { Text = "Msg_NoService".GetLocalized() },
                        new ProgressBar { IsIndeterminate = true },
                    },
                },
                XamlRoot = App.MainWindow.Content.XamlRoot,
            };

            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () => await loadingDialog.ShowAsync());
        }

        // Wait for service to become responsive
        var verifyResult = await VerifyServiceStartupAsync();

        if (serviceStartIssued && !verifyResult)
        {
            loadingDialog.Content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 32,
                Children =
                {
                    new InfoBar
                    {
                        Title = "ErrorOcurred_Title".GetLocalized(),
                        Severity = InfoBarSeverity.Error,
                        IsOpen = true,
                        IsClosable = false,
                        Message = "Msg_ServiceUnresponsive".GetLocalized(),
                    },
                },
            };
            return;
        }
        else if (serviceStartIssued && verifyResult)
        {
            loadingDialog.Hide();
        }

        // Only run at first startup
        if (!localSettingsService.GetValue<bool>("NotFirstRun"))
        {
            AutostartHandler.EnableAutoStart(App.MainWindow.Content.XamlRoot);
            SystemTimeFormat();
            await AddJumpListAsync();
            localSettingsService.SetValue("NotFirstRun", true);
        }
        else
        {
            AutostartHandler.EnsureAutostart(App.MainWindow.Content.XamlRoot);
        }

        // If language changed, add jumplist in new language
        if (localSettingsService.GetValue<bool>("LanguageChanged"))
        {
            await AddJumpListAsync();
            localSettingsService.SetValue("LanguageChanged", false);
        }
    }

    private void MoveWindow()
    {
        var isMainWindowMaximized = localSettingsService.GetValue<bool>("IsMainWindowMaximized");
        var positionX = localSettingsService.GetValue<int>("MainWindowPositionX");
        var positionY = localSettingsService.GetValue<int>("MainWindowPositionY");
        var width = localSettingsService.GetValue<int>("MainWindowWidth");
        var height = localSettingsService.GetValue<int>("MainWindowHeight");

        if (width is > 0 && height is > 0)
        {
            App.MainWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(positionX, positionY, width, height));
        }

        var presenter = App.MainWindow.AppWindow.Presenter as OverlappedPresenter;
        if (presenter is not null)
        {
            var state = isMainWindowMaximized ? OverlappedPresenterState.Maximized : OverlappedPresenterState.Restored;
            if (state == OverlappedPresenterState.Maximized)
            {
                presenter.Maximize();
            }
        }
    }

    private static async Task WaitForXamlRootAsync()
    {
        var tcs = new TaskCompletionSource();

        int attempts = 0;
        const int maxAttempts = 50;
        const int delayMs = 50;

        DispatcherQueue
            .GetForCurrentThread()
            .TryEnqueue(async () =>
            {
                while (attempts < maxAttempts)
                {
                    if (App.MainWindow.Content?.XamlRoot != null)
                    {
                        tcs.SetResult();
                        return;
                    }

                    attempts++;
                    await Task.Delay(delayMs);
                }

                tcs.SetException(new TimeoutException("MainWindow XamlRoot not available after waiting."));
            });

        await tcs.Task;
    }

    private static bool StartService()
    {
        if (!Debugger.IsAttached)
        {
            using Mutex serviceRunning = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
            if (serviceRunning.WaitOne(TimeSpan.FromMilliseconds(100), false))
            {
                using Process svc = new();
                svc.StartInfo.UseShellExecute = false;
                svc.StartInfo.FileName = Helper.ExecutionPathService;
                svc.StartInfo.CreateNoWindow = true;
                svc.Start();
                serviceRunning.ReleaseMutex();
                return true;
            }
        }
        return false;
    }

    private static async Task<bool> VerifyServiceStartupAsync()
    {
        const int maxRetries = 5;
        ApiResponse response = null!;
        for (int i = 0; i < maxRetries; i++)
        {
            response = await Task.Run(() => ApiResponse.FromString(MessageHandler.Client.SendMessageAndGetReply(Command.Alive)));
            if (response.StatusCode == StatusCode.Ok)
                break;
            await Task.Delay(1000);
        }

        if (response.StatusCode == StatusCode.Timeout)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void SystemTimeFormat()
    {
        string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        sysFormat = sysFormat[..sysFormat.IndexOf(':')];
        if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
        {
            localSettingsService.SetValue("TwelveHourClock", true);
        }
    }

    private static async Task AddJumpListAsync()
    {
        if (JumpList.IsSupported())
        {
            var jumpList = await JumpList.LoadCurrentAsync();

            jumpList.Items.Clear();

            var darkJumpTask = JumpListItem.CreateWithArguments(Command.Dark, "DarkTheme".GetLocalized());
            darkJumpTask.GroupName = "SwitchTheme".GetLocalized();

            var lightJumpTask = JumpListItem.CreateWithArguments(Command.Light, "LightTheme".GetLocalized());
            lightJumpTask.GroupName = "SwitchTheme".GetLocalized();

            jumpList.Items.Add(darkJumpTask);
            jumpList.Items.Add(lightJumpTask);

            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

            await jumpList.SaveAsync();
        }
    }
}
