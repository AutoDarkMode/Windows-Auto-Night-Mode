using System.Diagnostics;
using AutoDarkModeApp.Contracts.Services;
using AutoDarkModeApp.Helpers;
using AutoDarkModeSvc.Communication;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoDarkModeApp.Services;

public class ErrorService : IErrorService
{
    private static readonly Queue<DialogRequest> _dialogQueue = new();
    private static bool _isDialogShowing = false;

    public async Task ShowErrorMessageFromApi(ApiResponse response, Exception ex, XamlRoot xamlRoot)
    {
        var error = $@"{"ErrorMessageBox".GetLocalized()}

Exception Source: {ex.Source}
Exception Message: {ex.Message}

API Response:
Status Code: {response.StatusCode}
Message: {response.Message}
Details: {response.Details}";

        var request = new DialogRequest
        {
            Title = "ErrorOccurred_Title".GetLocalized(),
            Content = error,
            XamlRoot = xamlRoot,
        };

        await EnqueueDialog(request);
    }

    public async Task ShowErrorMessageFromApi(ApiResponse response, XamlRoot xamlRoot)
    {
        var error = $@"{"ErrorMessageBox".GetLocalized()}

            API Response:
            Status Code: {response.StatusCode}
    Message: {response.Message}
    Details: {response.Details}";

        var request = new DialogRequest
        {
            Title = "ErrorOccurred_Title".GetLocalized(),
            Content = error,
            XamlRoot = xamlRoot,
        };

        await EnqueueDialog(request);
    }

    public async Task ShowErrorMessage(Exception ex, XamlRoot xamlRoot, string location, string extraInfo = "")
    {
        var error = $@"{"ErrorMessageBox".GetLocalized()}
    
Error occurred in: {location}
Source: {ex.Source}
Message: {ex.Message}";

        if (!string.IsNullOrEmpty(extraInfo))
        {
            error += $"\nExtra Detail: {extraInfo}";
        }

        var request = new DialogRequest
        {
            Title = "ErrorOccurred_Title".GetLocalized(),
            Content = error,
            XamlRoot = xamlRoot,
        };

        await EnqueueDialog(request);
    }

    private static async Task EnqueueDialog(DialogRequest request)
    {
        _dialogQueue.Enqueue(request);

        if (!_isDialogShowing)
        {
            await ProcessDialogQueue();
        }
    }

    private static async Task ProcessDialogQueue()
    {
        while (_dialogQueue.Count > 0)
        {
            _isDialogShowing = true;
            var request = _dialogQueue.Dequeue();

            ContentDialog dialog = new()
            {
                Title = request.Title,
                Content = request.Content,
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = request.XamlRoot,
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri) { UseShellExecute = true, Verb = "open" });
            }
        }

        _isDialogShowing = false;
    }
}

public class DialogRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required XamlRoot XamlRoot { get; set; }
}

public class SwitchThemeException : Exception
{
    private static readonly string customMessage = "Theme switching is unsuccessful";

    public SwitchThemeException()
        : base(customMessage)
    {
        Source = "SwitchThemeException";
    }

    public SwitchThemeException(string message, string source)
        : base($"{customMessage}: {message}")
    {
        Source = source;
    }
}

public class AddAutoStartException : Exception
{
    public override string Message => "Auto start task could not been set.";

    public AddAutoStartException()
    {
        Source = "AutoStartException";
    }

    public AddAutoStartException(string message, string source)
        : base(message)
    {
        Source = source;
    }
}

public class AutoStartStatusGetException : Exception
{
    public override string Message => "Auto start info could not be retrieved.";
        
    public AutoStartStatusGetException()
    {
        Source = "AutoStartException";
    }

    public AutoStartStatusGetException(string message, string source)
        : base(message)
    {
        Source = source;
    }
}

public class RemoveAutoStartException : Exception
{
    public override string Message => "Auto start task could not be removed.";

    public RemoveAutoStartException()
    {
        Source = "RemoveAutoStartException";
    }

    public RemoveAutoStartException(string message, string source)
        : base(message)
    {
        Source = source;
    }
}
