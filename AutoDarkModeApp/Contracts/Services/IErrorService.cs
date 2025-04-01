using AutoDarkModeSvc.Communication;
using Microsoft.UI.Xaml;

namespace AutoDarkModeApp.Contracts.Services;

public interface IErrorService
{
    Task ShowErrorMessageFromApi(ApiResponse response, Exception ex, XamlRoot xamlRoot);
    Task ShowErrorMessageFromApi(ApiResponse response, XamlRoot xamlRoot);
    Task ShowErrorMessage(Exception ex, XamlRoot xamlRoot, string location, string extraInfo = "");
}
