using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeApp.Utils.Handlers;
using AutoDarkModeApp.ViewModels;
using AutoDarkModeLib;
using AutoDarkModeSvc.Communication;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Services;

public class ActivationService : IActivationService
{
    private readonly INavigationService _navigationService;
    private readonly ILocalSettingsService _localSettings;

    public ActivationService(INavigationService navigationService, ILocalSettingsService localSettingsService)
    {
        _navigationService = navigationService;
        _localSettings = localSettingsService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Navigate to default page
        _navigationService.NavigateTo(typeof(TimeViewModel).FullName!);

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
                Title = "StartupLaunchingServiceTitle".GetLocalized(),
                Content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 32,
                    Children =
                    {
                        new TextBlock { Text = "StartupLaunchingServiceText".GetLocalized() },
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
                        Title = "errorOcurredTitle".GetLocalized(),
                        Severity = InfoBarSeverity.Error,
                        IsOpen = true,
                        IsClosable = false,
                        Message = "StartupServiceUnresponsive".GetLocalized(),
                    },
                },
            };
        }
        else if (serviceStartIssued && verifyResult)
        {
            loadingDialog.Hide();
        }
    }

    private async Task MoveWindowAsync()
    {
        var left = await _localSettings.ReadSettingAsync<int>("X");
        var top = await _localSettings.ReadSettingAsync<int>("Y");
        var width = await _localSettings.ReadSettingAsync<int>("Width");
        var height = await _localSettings.ReadSettingAsync<int>("Height");
        App.MainWindow.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(left, top, width, height));
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
        if (Debugger.IsAttached)
        {
            using Mutex serviceRunning = new(false, "330f929b-ac7a-4791-9958-f8b9268ca35d");
            if (serviceRunning.WaitOne(TimeSpan.FromMilliseconds(100), false))
            {
                using Process svc = new();
                svc.StartInfo.UseShellExecute = false;
                svc.StartInfo.FileName = Path.Combine(Helper.ExecutionDir, "AutoDarkModeSvc.exe");
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
        string response = null!;
        for (int i = 0; i < maxRetries; i++)
        {
            response = await Task.Run(() => MessageHandler.Client.SendMessageAndGetReply(Command.Alive));
            await Task.Delay(1000);
        }
        return !response.Contains(StatusCode.Timeout);
    }
}
