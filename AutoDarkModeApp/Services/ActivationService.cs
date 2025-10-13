using System.Diagnostics;
using System.Globalization;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.StartScreen;

namespace AutoDarkModeApp.Services;

public class ActivationService(ILocalSettingsService localSettingsService, INavigationService navigationService) : IActivationService
{
    public async Task ActivateAsync(object activationArgs)
    {
        // Navigate to default page
        navigationService.NavigateTo(typeof(TimeViewModel).FullName!);

        // Move window to config position
        await MoveWindowAsync();

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
        if (!await localSettingsService.ReadSettingAsync<bool>("NotFirstRun"))
        {
            Debug.WriteLine("first-run");

            AutostartHandler.EnableAutoStart(App.MainWindow.Content.XamlRoot);
            await SystemTimeFormatAsync();
            await AddJumpListAsync();
            await localSettingsService.SaveSettingAsync("NotFirstRun", true);
        }
        else
        {
            AutostartHandler.EnsureAutostart(App.MainWindow.Content.XamlRoot);
        }

        // If language changed, add jumplist in new language
        if (await localSettingsService.ReadSettingAsync<bool>("LanguageChanged"))
        {
            await AddJumpListAsync();
            await localSettingsService.SaveSettingAsync("LanguageChanged", false);
        }
    }

    private async Task MoveWindowAsync()
    {
        var left = await localSettingsService.ReadSettingAsync<int>("X");
        var top = await localSettingsService.ReadSettingAsync<int>("Y");
        var width = await localSettingsService.ReadSettingAsync<int>("Width");
        var height = await localSettingsService.ReadSettingAsync<int>("Height");
        var windowState = await localSettingsService.ReadSettingAsync<int>("WindowState");

        App.MainWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(left, top, width, height));

        var presenter = App.MainWindow.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            var state = (OverlappedPresenterState)windowState;
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

    private async Task SystemTimeFormatAsync()
    {
        string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        sysFormat = sysFormat[..sysFormat.IndexOf(':')];
        if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
        {
            await localSettingsService.SaveSettingAsync("TwelveHourClock", true);
        }
    }

    private static async Task AddJumpListAsync()
    {
        if (JumpList.IsSupported())
        {
            var jumpList = await JumpList.LoadCurrentAsync();

            jumpList.Items.Clear();
            Debug.WriteLine("Adding jumplist items");
            Debug.WriteLine($"Current override: {Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride}");

            var darkJumpTask = JumpListItem.CreateWithArguments(Command.Dark, "DarkTheme".GetLocalized());
            darkJumpTask.GroupName = "SwitchTheme".GetLocalized();

            var lightJumpTask = JumpListItem.CreateWithArguments(Command.Light, "LightTheme".GetLocalized());
            lightJumpTask.GroupName = "SwitchTheme".GetLocalized();

            var resetJumpTask = JumpListItem.CreateWithArguments(Command.NoForce, "Reset".GetLocalized());
            resetJumpTask.GroupName = "SwitchTheme".GetLocalized();

            jumpList.Items.Add(darkJumpTask);
            jumpList.Items.Add(lightJumpTask);
            jumpList.Items.Add(resetJumpTask);

            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

            await jumpList.SaveAsync();
        }
    }
}
